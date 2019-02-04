#include "max30102_controller.h"
#include "max30102_algox.h"
#include "nrf_log.h"
#include "nrf_log_ctrl.h"
#include "max30102_driver.h"
#include "arm_math.h"
#include "app_timer.h"
#include "myuart.h"

arm_rfft_fast_instance_f32 rfft_instance;

static float raw_red_data[RAW_DATA_BUFFER_SIZE];
static float fft_complex_output[RAW_DATA_BUFFER_SIZE*2];
static int16_t raw_data_write_index = 0;
static bool is_activated = false;

static void fifo_read_timeout_handler(void * p_context);

APP_TIMER_DEF(m_fifo_read_timer_id);

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
    arm_max_f32(raw_red_data, RAW_DATA_BUFFER_SIZE/2, &maxValue, &energyIndex);

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


void max30102_sensor_init(void)
{
    if (maxim_twi_init() != 0)
    {
        NRF_LOG_INFO("twi init failed.");
        APP_ERROR_CHECK_BOOL(false);
    }

    if (!maxim_max30102_reset())
    {
        NRF_LOG_INFO("maxim_max30102_reset failed.");
        APP_ERROR_CHECK_BOOL(false);
    }

    //read and clear status register
    uint8_t uch_dummy;
    maxim_max30102_read_reg(0,&uch_dummy);
    maxim_max30102_read_reg(1,&uch_dummy);

//    if (!maxim_max30102_init())
//    {
//        NRF_LOG_INFO("maxim_max30102_init failed.");
//        APP_ERROR_CHECK_BOOL(false);
//    }

    ret_code_t      err_code;
    err_code = app_timer_create(&m_fifo_read_timer_id,
                                APP_TIMER_MODE_SINGLE_SHOT,
                                fifo_read_timeout_handler);

    APP_ERROR_CHECK(err_code);
}

void max30102_sensor_start(void)
{
    if (!maxim_max30102_reset())
    {
        NRF_LOG_INFO("maxim_max30102_reset failed.");
        APP_ERROR_CHECK_BOOL(false);
    }

    //read and clear status register
    uint8_t uch_dummy;
    maxim_max30102_read_reg(0,&uch_dummy);
    maxim_max30102_read_reg(1,&uch_dummy);

    if (!maxim_max30102_init())
    {
        NRF_LOG_INFO("maxim_max30102_init failed.");
        APP_ERROR_CHECK_BOOL(false);
    }

    // start timer
    ret_code_t      err_code;
    err_code = app_timer_start(m_fifo_read_timer_id, FIFO_READ_PERIOD_TICKS, NULL);
    APP_ERROR_CHECK(err_code);

    is_activated = true;
}

void max30102_sensor_stop(void)
{
    is_activated = false;
    if (!maxim_max30102_shutdown())
    {
        NRF_LOG_INFO("maxim_max30102_shutdown failed.");
        APP_ERROR_CHECK_BOOL(false);
    }
}

static void fifo_read_timeout_handler(void * p_context)
{
    if (!is_activated)
    {
        return;
    }

    uint32_t red, ired;
    ret_code_t      err_code;

    if (!maxim_max30102_data_ready())   //wait until the interrupt pin asserts
    {
        err_code = app_timer_start(m_fifo_read_timer_id, FIFO_READ_PERIOD_TICKS, NULL);
        APP_ERROR_CHECK(err_code);

        return;
    }

    maxim_max30102_read_fifo(&red, &ired);

    feed_red_ired(red, ired);

    myuart_printf("r:%i\r\n", red);
    myuart_printf("ir:%i\r\n", ired);

    // re-start timer
    err_code = app_timer_start(m_fifo_read_timer_id, FIFO_READ_PERIOD_TICKS, NULL);
    APP_ERROR_CHECK(err_code);
}
