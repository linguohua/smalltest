#include "myuart.h"
#include "nrf_drv_uart.h"
#include "app_error.h"
#include "string.h"
#include "nrf_fprintf_format.h"
#include <stdarg.h>

nrf_drv_uart_t m_uart = NRF_DRV_UART_INSTANCE(0);

static uint8_t m_string_buff[NRF_LOG_BACKEND_UART_TEMP_BUFFER_SIZE];
static volatile bool m_xfer_done;
static bool m_async_mode;

static void serial_tx(void const * p_context, char const * p_buffer, size_t len);
NRF_FPRINTF_DEF(printfCtx, 0, m_string_buff, sizeof(m_string_buff), true, serial_tx);

static void uart_evt_handler(nrf_drv_uart_event_t * p_event, void * p_context)
{
    m_xfer_done = true;
}

void myuart_init(bool async_mode)
{
    nrf_drv_uart_config_t config = NRF_DRV_UART_DEFAULT_CONFIG;
    config.pseltxd  = NRF_LOG_BACKEND_UART_TX_PIN;
    config.pselrxd  = NRF_UART_PSEL_DISCONNECTED;
    config.pselcts  = NRF_UART_PSEL_DISCONNECTED;
    config.pselrts  = NRF_UART_PSEL_DISCONNECTED;
    config.baudrate = (nrf_uart_baudrate_t)NRF_LOG_BACKEND_UART_BAUDRATE;
    ret_code_t err_code = nrf_drv_uart_init(&m_uart, &config, async_mode ? uart_evt_handler : NULL);
    APP_ERROR_CHECK(err_code);

    m_async_mode = async_mode;
}

static void serial_tx(void const * p_context, char const * p_buffer, size_t len)
{
    uint8_t len8 = (uint8_t)(len & 0x000000FF);
    m_xfer_done = false;
    ret_code_t err_code = nrf_drv_uart_tx(&m_uart, (uint8_t *)p_buffer, len8);
    APP_ERROR_CHECK(err_code);
    /* wait for completion since buffer is reused*/
    while (m_async_mode && (m_xfer_done == false))
    {

    }

}

void myuart_write(char const * p_buffer, size_t len)
{
    uint8_t copy = len;
    if (copy > NRF_LOG_BACKEND_UART_TEMP_BUFFER_SIZE)
    {
        copy = NRF_LOG_BACKEND_UART_TEMP_BUFFER_SIZE;
    }

    memcpy_fast(m_string_buff, p_buffer, copy);
    serial_tx(0, m_string_buff, copy);
}

void myuart_printf(char const *fmt, ...)
{

    if (fmt == NULL)
    {
        return;
    }

    va_list args = {0};
    va_start(args, fmt);

    nrf_fprintf_fmt(&printfCtx, fmt, &args);

    va_end(args);
}
