#include "voice.h"
#include "nrf_gpio.h"
#include "app_timer.h"

#include "nrf_log.h"

#define SDA_DATA_BYTE_SIZE 4

#define HIGH_SDA() nrf_gpio_pin_write(VOICE_SDA_PIN, 1)
#define LOW_SDA() nrf_gpio_pin_write(VOICE_SDA_PIN, 0)

//#define HIGH_BUSY()
//#define LOW_BUSY()
//#define HIGH_SDA()
//#define LOW_SDA()

typedef enum
{
    VOICE_SEND_STATE_INIT = 0,
    VOICE_SEND_STATE_BIT_FIRST_PART,
    VOICE_SEND_STATE_BIT_SECOND_PART,
    VOICE_SEND_STATE_BYTE_END
} voice_send_state_t;

static bool m_is_in_transform_mode = false;
static unsigned char m_total_seq_length = 0;
static unsigned char m_sent_length = 0;
static unsigned char m_sent_bits = 0;
static voice_send_state_t m_voice_send_state = VOICE_SEND_STATE_INIT;
static unsigned char m_data_to_send[SDA_DATA_BYTE_SIZE];

APP_TIMER_DEF(m_voice_send_timer_id);

static uint32_t voice_start_tranform(void)
{
    if (m_is_in_transform_mode)
    {
        return NRF_ERROR_INVALID_STATE;
    }

    if (m_total_seq_length < 1 ||
        m_total_seq_length > SDA_DATA_BYTE_SIZE)
    {
        return NRF_ERROR_INVALID_PARAM;
    }

    m_sent_length = 0;
    m_sent_bits = 0;

    // GPIO state set
    LOW_SDA();

    // start timer
    ret_code_t      err_code;
    err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_START_LOW_TICKS, NULL);
    APP_ERROR_CHECK(err_code);

    m_voice_send_state = VOICE_SEND_STATE_INIT;
    m_is_in_transform_mode = true;

    return NRF_SUCCESS;
}

static void voice_send_bit_first_part()
{
    unsigned char current_byte = m_data_to_send[m_sent_length];
    unsigned char current_bit = ((current_byte >> m_sent_bits)&0x01);

    ret_code_t      err_code;

    // high SDA
    HIGH_SDA();

    if (current_bit)
    {
        // 3t:1t represent 1
        err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_DATA_LONG_PART, NULL);
        APP_ERROR_CHECK(err_code);
    }
    else
    {
        // 1t:3t represent 0
        err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_DATA_SHORT_PART, NULL);
        APP_ERROR_CHECK(err_code);
    }

    m_voice_send_state = VOICE_SEND_STATE_BIT_FIRST_PART;
}

static void voice_send_bit_second_part()
{
    unsigned char current_byte = m_data_to_send[m_sent_length];
    unsigned char current_bit = ((current_byte >> m_sent_bits)&0x01);

    ret_code_t      err_code;

    // low SDA
    LOW_SDA();

    if (current_bit)
    {
        // 3t:1t represent 1
        err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_DATA_SHORT_PART, NULL);
        APP_ERROR_CHECK(err_code);
    }
    else
    {
        // 1t:3t represent 0
        err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_DATA_LONG_PART, NULL);
        APP_ERROR_CHECK(err_code);
    }

    m_voice_send_state = VOICE_SEND_STATE_BIT_SECOND_PART;
}

static void voice_send_byte_end()
{
    HIGH_SDA();

    ret_code_t      err_code;
    err_code = app_timer_start(m_voice_send_timer_id, VOICE_SDA_DATA_SHORT_PART, NULL);
    APP_ERROR_CHECK(err_code);

    m_voice_send_state = VOICE_SEND_STATE_BYTE_END;
}

static void voice_send_timeout_handler(void * p_context)
{
    //app_timer_stop(m_voice_send_timer_id);
    switch(m_voice_send_state)
    {
    case VOICE_SEND_STATE_INIT:
        voice_send_bit_first_part();
    break;
    case VOICE_SEND_STATE_BIT_FIRST_PART:
        voice_send_bit_second_part();
    break;
    case VOICE_SEND_STATE_BIT_SECOND_PART:
    {
        m_sent_bits++;
        if (m_sent_bits == 8)
        {
            voice_send_byte_end();

            return;
        }

        voice_send_bit_first_part();
    }
    break;
    case VOICE_SEND_STATE_BYTE_END:
    {
        // send 8 bits completed
        m_sent_length++;

        if (m_sent_length == m_total_seq_length)
        {
            NRF_LOG_INFO("voice send all end");

            // send completed
            m_is_in_transform_mode = false;

            return;
        }

        // new byte
        m_sent_bits = 0;
        voice_send_bit_first_part();
    }
    break;
    default:
        ASSERT("voice_send_timeout_handler SHOULD NOT BE HERE" == NULL);
    break;
    }
}

uint32_t voice_init()
{
    ret_code_t      err_code;

    nrf_gpio_cfg_output(VOICE_SDA_PIN);

    nrf_gpio_pin_write(VOICE_SDA_PIN, 1); // make SDA high, stop data transform

    err_code = app_timer_create(&m_voice_send_timer_id,
                                APP_TIMER_MODE_SINGLE_SHOT,
                                voice_send_timeout_handler);

    APP_ERROR_CHECK(err_code);

    return err_code;
}

uint32_t voice_play(uint32_t recordID)
{
    if (recordID >= 233)
    {
        // the record index exceed the maximum allowed by voice-IC
        // see the datasheet at: http://www.stchip.com/upload/file/1428047653.pdf
        return NRF_ERROR_INVALID_PARAM;
    }

    m_data_to_send[0] = (unsigned char)recordID;
    m_total_seq_length = 1;

    voice_start_tranform();

    return NRF_SUCCESS;
}

uint32_t voice_stop()
{
    return NRF_SUCCESS;
}
