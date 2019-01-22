#include "max30102_controller.h"
#include "max30102_algox.h"
#include "nrf_log.h"
#include "nrf_log_ctrl.h"

#include "arm_math.h"

arm_rfft_fast_instance_f32 rfft_instance;

static float raw_red_data[RAW_DATA_BUFFER_SIZE];
static float fft_complex_output[RAW_DATA_BUFFER_SIZE*2];
static int16_t raw_data_write_index = 0;

static void do_hr_calc()
{
    float maxValue;
    uint32_t energyIndex;

    // move average with window size 11
    move_average(11, raw_red_data, RAW_DATA_BUFFER_SIZE, fft_complex_output);

    // move average with window size 101
    move_average(101, fft_complex_output, RAW_DATA_BUFFER_SIZE, fft_complex_output+RAW_DATA_BUFFER_SIZE);

    // calc osc
    array_sub(fft_complex_output, RAW_DATA_BUFFER_SIZE, fft_complex_output+RAW_DATA_BUFFER_SIZE, raw_red_data);

    arm_rfft_fast_init_f32(&rfft_instance, RAW_DATA_BUFFER_SIZE);

    // fft
    /* Process the data through the CFFT/CIFFT module */
    arm_rfft_fast_f32(&rfft_instance, raw_red_data, fft_complex_output, 0);

    /* Process the data through the Complex Magnitude Module for
    calculating the magnitude at each bin */
    arm_cmplx_mag_f32(fft_complex_output, raw_red_data, RAW_DATA_BUFFER_SIZE);

    // max magitude
    // max magitude freq
    /* Calculates maxValue and returns corresponding BIN value */
    arm_max_f32(raw_red_data, RAW_DATA_BUFFER_SIZE, &maxValue, &energyIndex);

    // heart rate calc
    float hr = ((float)energyIndex*SAMPLE_RATE/(float)RAW_DATA_BUFFER_SIZE)*60.0f;

    // output result
    NRF_LOG_INFO("energyIndex:%i\r\n", energyIndex);   
    NRF_LOG_INFO("hr:%i\r\n", (int)hr);
    NRF_LOG_FLUSH();
}

void feed_red_ired(float red, float ired)
{
    raw_red_data[raw_data_write_index] = red;
    raw_data_write_index++;

    if (raw_data_write_index == RAW_DATA_BUFFER_SIZE)
    {
        // roll back
        raw_data_write_index = 0;

        do_hr_calc();
    }
}
