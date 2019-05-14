#include "max30102_controller.h"
#include "max30102_algox.h"
#include "arm_math.h"
#include "math.h"
#include "nrf_log.h"
#include "nrf_log_ctrl.h"

#if !USE_FFT
#define HAMMING_SIZE  5// DO NOT CHANGE
const uint16_t auw_hamm[31]={ 41,    276,    512,    276,     41 }; //Hamm=  long16(512* hamming(5)');
static float average_output[RAW_DATA_BUFFER_SIZE*2];

void hr_calc_init()
{
}

static void maxim_sort_ascend(int32_t *pn_x,int32_t n_size) 
/**
* \brief        Sort array
* \par          Details
*               Sort array in ascending order (insertion sort algorithm)
*
* \retval       None
*/
{
    int32_t i, j, n_temp;
    for (i = 1; i < n_size; i++) {
        n_temp = pn_x[i];
        for (j = i; j > 0 && n_temp < pn_x[j-1]; j--)
            pn_x[j] = pn_x[j-1];
        pn_x[j] = n_temp;
    }
}

static void maxim_sort_indices_descend(float *pn_x, int32_t *pn_indx, int32_t n_size)
/**
* \brief        Sort indices
* \par          Details
*               Sort indices according to descending order (insertion sort algorithm)
*
* \retval       None
*/ 
{
    int32_t i, j, n_temp;
    for (i = 1; i < n_size; i++) {
        n_temp = pn_indx[i];
        for (j = i; j > 0 && pn_x[n_temp] > pn_x[pn_indx[j-1]]; j--)
            pn_indx[j] = pn_indx[j-1];
        pn_indx[j] = n_temp;
    }
}

static void maxim_peaks_above_min_height(int32_t *pn_locs, int32_t *pn_npks, float  *pn_x, int32_t n_size, int32_t n_min_height)
/**
* \brief        Find peaks above n_min_height
* \par          Details
*               Find all peaks above MIN_HEIGHT
*
* \retval       None
*/
{
    int32_t i = 1, n_width;
    *pn_npks = 0;
    
    while (i < n_size-1){
        if (pn_x[i] > n_min_height && pn_x[i] > pn_x[i-1]){            // find left edge of potential peaks
            n_width = 1;
            while (i+n_width < n_size && pn_x[i] == pn_x[i+n_width])    // find flat peaks
                n_width++;
            if (pn_x[i] > pn_x[i+n_width] && (*pn_npks) < 15 ){                            // find right edge of peaks
                pn_locs[(*pn_npks)++] = i;        
                // for flat peaks, peak location is left edge
                i += n_width+1;
            }
            else
                i += n_width;
        }
        else
            i++;
    }
}


static void maxim_remove_close_peaks(int32_t *pn_locs, int32_t *pn_npks, float *pn_x, int32_t n_min_distance)
/**
* \brief        Remove peaks
* \par          Details
*               Remove peaks separated by less than MIN_DISTANCE
*
* \retval       None
*/
{
    
    int32_t i, j, n_old_npks, n_dist;
    
    /* Order peaks from large to small */
    maxim_sort_indices_descend( pn_x, pn_locs, *pn_npks );

    for ( i = -1; i < *pn_npks; i++ ){
        n_old_npks = *pn_npks;
        *pn_npks = i+1;
        for ( j = i+1; j < n_old_npks; j++ ){
            n_dist =  pn_locs[j] - ( i == -1 ? -1 : pn_locs[i] ); // lag-zero peak of autocorr is at index -1
            if ( n_dist > n_min_distance || n_dist < -n_min_distance )
                pn_locs[(*pn_npks)++] = pn_locs[j];
        }
    }

    // Resort indices longo ascending order
    maxim_sort_ascend( pn_locs, *pn_npks );
}

static void maxim_find_peaks(int32_t *pn_locs, int32_t *pn_npks, float *pn_x, int32_t n_size, int32_t n_min_height, int32_t n_min_distance, int32_t n_max_num)
/**
* \brief        Find peaks
* \par          Details
*               Find at most MAX_NUM peaks above MIN_HEIGHT separated by at least MIN_DISTANCE
*
* \retval       None
*/
{
    maxim_peaks_above_min_height( pn_locs, pn_npks, pn_x, n_size, n_min_height );
    maxim_remove_close_peaks( pn_locs, pn_npks, pn_x, n_min_distance );
    if (*pn_npks > n_max_num)
    {
        *pn_npks = n_max_num;
    }
}

int hr_calc(float* raw_red_data)
{
    float s;
    int i;
    int k;
    int hr;
    int n_peak_interval_sum;
    int32_t an_dx_peak_locs[15];
    int n_npks = 0;
    int n_th1;

    // move average with window size 11
    move_average(11, raw_red_data, RAW_DATA_BUFFER_SIZE, average_output);

    // move average with window size 101
    move_average(101, average_output, RAW_DATA_BUFFER_SIZE, average_output+RAW_DATA_BUFFER_SIZE);

    // calc osc
    array_sub(average_output, RAW_DATA_BUFFER_SIZE, average_output+RAW_DATA_BUFFER_SIZE, average_output);

    // hamming window
    // flip wave form so that we can detect valley with peak detector
    for ( i=0 ; i<RAW_DATA_BUFFER_SIZE-HAMMING_SIZE ;i++){
        s= 0;
        for( k=i; k<i+ HAMMING_SIZE ;k++){
            s -= average_output[k] *auw_hamm[k-i] ; 
                     }
        average_output[i]= s/ (float)1146; // divide by sum of auw_hamm 
    }

    n_th1=0; // threshold calculation
    for ( k=0 ; k < RAW_DATA_BUFFER_SIZE-HAMMING_SIZE ;k++){
        n_th1 += ((average_output[k]>0)? average_output[k] : ((int32_t)0-average_output[k])) ;
    }
    n_th1= n_th1/ ( RAW_DATA_BUFFER_SIZE-HAMMING_SIZE);
    NRF_LOG_INFO("n_th1:%i\r\n", n_th1);
    // average osci too small
    if (n_th1 < 300) {
      return 0;
    }

    // peak location is acutally index for sharpest location of raw signal since we flipped the signal         
    maxim_find_peaks( an_dx_peak_locs, &n_npks, average_output, RAW_DATA_BUFFER_SIZE-HAMMING_SIZE, n_th1, 25, 5 );//peak_height, peak_distance, max_num_peaks 

    n_peak_interval_sum =0;
    if (n_npks>=2){
        for (k=1; k<n_npks; k++)
            n_peak_interval_sum += (an_dx_peak_locs[k]-an_dx_peak_locs[k -1]);
        n_peak_interval_sum=n_peak_interval_sum/(n_npks-1);
        hr=(int32_t)(6000/n_peak_interval_sum);// beats per minutes
    }
    else  {
        hr = 0;
    }

    return hr;
}
#endif
