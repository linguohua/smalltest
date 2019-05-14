#include "max30102_controller.h"
#include "max30102_algox.h"
#include "arm_math.h"
#include "nrf_log.h"
#include "nrf_log_ctrl.h"

#if USE_FFT

static arm_rfft_fast_instance_f32 rfft_instance;
static float calc_buf[RAW_DATA_BUFFER_SIZE];
static float fft_complex_output[RAW_DATA_BUFFER_SIZE*2];

void hr_calc_init()
{
    arm_rfft_fast_init_f32(&rfft_instance, RAW_DATA_BUFFER_SIZE);
}

int hr_calc(float* raw_red_data)
{
    float maxValue;
    uint32_t energyIndex;
    ret_code_t      err_code;

    // move average with window size 11
    move_average(11, raw_red_data, RAW_DATA_BUFFER_SIZE, fft_complex_output);

    // move average with window size 101
    move_average(101, fft_complex_output, RAW_DATA_BUFFER_SIZE, fft_complex_output+RAW_DATA_BUFFER_SIZE);

    // calc osc
    array_sub(fft_complex_output, RAW_DATA_BUFFER_SIZE, fft_complex_output+RAW_DATA_BUFFER_SIZE, calc_buf);

    // fft
    /* Process the data through the CFFT/CIFFT module */
    arm_rfft_fast_f32(&rfft_instance, calc_buf, fft_complex_output, 0);

    /* Process the data through the Complex Magnitude Module for
    calculating the magnitude at each bin */
    arm_cmplx_mag_f32(fft_complex_output+2, calc_buf+1, RAW_DATA_BUFFER_SIZE/2-1);
    calc_buf[0]          = 0; // DC set to zero
    calc_buf[RAW_DATA_BUFFER_SIZE/2]          = 0; // DC set to zero

    // max magitude
    // max magitude freq
    /* Calculates maxValue and returns corresponding BIN value */
    arm_max_f32(calc_buf, RAW_DATA_BUFFER_SIZE/2, &maxValue, &energyIndex);

    // heart rate calc
    float hr = (((float)energyIndex)*(float)SAMPLE_RATE*60.0f/(float)RAW_DATA_BUFFER_SIZE);

    // output result
    NRF_LOG_INFO("maxValue:%i\r\n", (int)maxValue);
    NRF_LOG_INFO("energyIndex:%i\r\n", energyIndex);

    NRF_LOG_FLUSH();

    if (maxValue < 10000.0f || (hr < 45 || hr > 200))
    {
        hr = 0;
    }

    NRF_LOG_INFO("hr:%i\r\n", (int)hr);

    return (int)hr;
}
#endif
