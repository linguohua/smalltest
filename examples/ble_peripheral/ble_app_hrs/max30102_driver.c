#include "max30102_driver.h"
#include "nrf.h"
#include "app_error.h"
#include "boards.h"
#include "app_util_platform.h"
#include "app_error.h"
#include "nrf_drv_twi.h"
#include "nrf_log.h"

//I2C i2c(I2C_SDA, I2C_SCL);//SDA-PB9,SCL-PB8

#define TWI_INSTANCE_ID 0

/* TWI instance. */
static const nrf_drv_twi_t m_twi = NRF_DRV_TWI_INSTANCE(TWI_INSTANCE_ID);

/** Write to an I2C slave
 *
 * Performs a complete write transaction. The bottom bit of
 * the address is forced to 0 to indicate a write.
 *
 *  @param address 8-bit I2C slave address [ addr | 0 ]
 *  @param data Pointer to the byte-array data to send
 *  @param length Number of bytes to send
 *  @param repeated Repeated start, true - do not send stop at end
 *
 *  @returns
 *       0 on success (ack),
 *   non-0 on failure (nack)
 */
int i2c_write(int address, const char *data, int length, int repeated)
{
    ret_code_t err_code;
    err_code = nrf_drv_twi_tx(&m_twi, (uint8_t)address, (const uint8_t*)data, (uint8_t)length, repeated);
    APP_ERROR_CHECK(err_code);

    return err_code;
}

/** Read from an I2C slave
 *
 * Performs a complete read transaction. The bottom bit of
 * the address is forced to 1 to indicate a read.
 *
 *  @param address 8-bit I2C slave address [ addr | 1 ]
 *  @param data Pointer to the byte-array to read data in to
 *  @param length Number of bytes to read
 *  @param repeated Repeated start, true - don't send stop at end
 *
 *  @returns
 *       0 on success (ack),
 *   non-0 on failure (nack)
 */
int i2c_read(int address, char *data, int length, int repeated)
{
    ret_code_t err_code = NRF_SUCCESS;
    err_code = nrf_drv_twi_rx(&m_twi, (uint8_t)address, (uint8_t*)data, (uint8_t)length);
    APP_ERROR_CHECK(err_code);

    return err_code;
}

bool maxim_max30102_write_reg(uint8_t uch_addr, uint8_t uch_data)
/**
* \brief        Write a value to a MAX30102 register
* \par          Details
*               This function writes a value to a MAX30102 register
*
* \param[in]    uch_addr    - register address
* \param[in]    uch_data    - register data
*
* \retval       true on success
*/
{
  char ach_i2c_data[2];
  ach_i2c_data[0]=uch_addr;
  ach_i2c_data[1]=uch_data;

  if(i2c_write(I2C_WRITE_ADDR, ach_i2c_data, 2, true)==0)
    return true;
  else
    return false;
}

bool maxim_max30102_read_reg(uint8_t uch_addr, uint8_t *puch_data)
/**
* \brief        Read a MAX30102 register
* \par          Details
*               This function reads a MAX30102 register
*
* \param[in]    uch_addr    - register address
* \param[out]   puch_data    - pointer that stores the register data
*
* \retval       true on success
*/
{
  char ch_i2c_data;
  ch_i2c_data=uch_addr;
  if(i2c_write(I2C_WRITE_ADDR, &ch_i2c_data, 1, true)!=0)
    return false;
  if(i2c_read(I2C_READ_ADDR, &ch_i2c_data, 1, false)==0)
  {
    *puch_data=(uint8_t) ch_i2c_data;
    return true;
  }
  else
    return false;
}

bool maxim_max30102_init()
/**
* \brief        Initialize the MAX30102
* \par          Details
*               This function initializes the MAX30102
*
* \param        None
*
* \retval       true on success
*/
{
//  uint8_t temp = 0xcc;
  if(!maxim_max30102_write_reg(REG_INTR_ENABLE_1,0x40)) // INTR setting, 0xc0
    return false;

//  if (!maxim_max30102_read_reg(REG_INTR_ENABLE_1, &temp))
//    return false;
//
//  if (temp != 0x40)
//    return false;

  if(!maxim_max30102_write_reg(REG_INTR_ENABLE_2,0x00))
    return false;

//  if (!maxim_max30102_read_reg(REG_INTR_ENABLE_2, &temp))
//    return false;
//
//  if (temp != 0x00)
//    return false;

  if(!maxim_max30102_write_reg(REG_FIFO_WR_PTR,0x00))  //FIFO_WR_PTR[4:0]
    return false;
  if(!maxim_max30102_write_reg(REG_OVF_COUNTER,0x00))  //OVF_COUNTER[4:0]
    return false;
  if(!maxim_max30102_write_reg(REG_FIFO_RD_PTR,0x00))  //FIFO_RD_PTR[4:0]
    return false;
  if(!maxim_max30102_write_reg(REG_FIFO_CONFIG,0x1f))  //sample avg = 1, fifo rollover=false, fifo almost full = 17, 0x0f
    return false;

//  if (!maxim_max30102_read_reg(REG_FIFO_CONFIG, &temp))
//    return false;
//
//  if (temp != 0x1f)
//    return false;

  if(!maxim_max30102_write_reg(REG_MODE_CONFIG,0x03))   //0x02 for Red only, 0x03 for SpO2 mode 0x07 multimode LED
    return false;

//  if (!maxim_max30102_read_reg(REG_MODE_CONFIG, &temp))
//    return false;
//
//  if (temp != 0x03)
//    return false;

  if(!maxim_max30102_write_reg(REG_SPO2_CONFIG,0x27))  // SPO2_ADC range = 4096nA, SPO2 sample rate (100 Hz), LED pulseWidth (400uS)
    return false;

//  if (!maxim_max30102_read_reg(REG_SPO2_CONFIG, &temp))
//    return false;
//
//  if (temp != 0x27)
//    return false;

  if(!maxim_max30102_write_reg(REG_LED1_PA,0x24))   //Choose value for ~ 7mA for LED1, RED
    return false;
  if(!maxim_max30102_write_reg(REG_LED2_PA,0x24))   // Choose value for ~ 7mA for LED2, IR, change from 0x24 to 0x30, 10mA
    return false;
//  if(!maxim_max30102_write_reg(REG_PILOT_PA,0x7f))   // Choose value for ~ 25mA for Pilot LED
//    return false;

//  if (!maxim_max30102_read_reg(REG_PILOT_PA, &temp))
//    return false;
//
//  if (temp != 0x7f)
//    return false;

  return true;
}

bool maxim_max30102_read_fifo(uint32_t *pun_red_led, uint32_t *pun_ir_led)
/**
* \brief        Read a set of samples from the MAX30102 FIFO register
* \par          Details
*               This function reads a set of samples from the MAX30102 FIFO register
*
* \param[out]   *pun_red_led   - pointer that stores the red LED reading data
* \param[out]   *pun_ir_led    - pointer that stores the IR LED reading data
*
* \retval       true on success
*/
{
  uint32_t un_temp;
  unsigned char uch_temp;
  *pun_red_led=0;
  *pun_ir_led=0;
  char ach_i2c_data[6];

  //read and clear status register
  maxim_max30102_read_reg(REG_INTR_STATUS_1, &uch_temp);
  maxim_max30102_read_reg(REG_INTR_STATUS_2, &uch_temp);

  ach_i2c_data[0]=REG_FIFO_DATA;
  if(i2c_write(I2C_WRITE_ADDR, ach_i2c_data, 1, true)!=0)
    return false;
  if(i2c_read(I2C_READ_ADDR, ach_i2c_data, 6, false)!=0)
  {
    return false;
  }
  un_temp=(unsigned char) ach_i2c_data[0];
  un_temp<<=16;
  *pun_red_led+=un_temp;
  un_temp=(unsigned char) ach_i2c_data[1];
  un_temp<<=8;
  *pun_red_led+=un_temp;
  un_temp=(unsigned char) ach_i2c_data[2];
  *pun_red_led+=un_temp;

  un_temp=(unsigned char) ach_i2c_data[3];
  un_temp<<=16;
  *pun_ir_led+=un_temp;
  un_temp=(unsigned char) ach_i2c_data[4];
  un_temp<<=8;
  *pun_ir_led+=un_temp;
  un_temp=(unsigned char) ach_i2c_data[5];
  *pun_ir_led+=un_temp;
  *pun_red_led&=0x03FFFF;  //Mask MSB [23:18]
  *pun_ir_led&=0x03FFFF;  //Mask MSB [23:18]


  return true;
}

bool maxim_max30102_reset()
/**
* \brief        Reset the MAX30102
* \par          Details
*               This function resets the MAX30102
*
* \param        None
*
* \retval       true on success
*/
{
    if(!maxim_max30102_write_reg(REG_MODE_CONFIG,0x40))
        return false;
    else
        return true;
}

uint32_t maxim_twi_init(void)
{
    ret_code_t err_code;
    nrf_gpio_cfg_input(MAX30102_INT_PIN, NRF_GPIO_PIN_NOPULL);

    const nrf_drv_twi_config_t twi_max30102_config = {
       .scl                = MAX30102_SCL_PIN,
       .sda                = MAX30102_SDA_PIN,
       .frequency          = NRF_DRV_TWI_FREQ_100K,
       .interrupt_priority = APP_IRQ_PRIORITY_HIGH,
       .clear_bus_init     = false
    };

    err_code = nrf_drv_twi_init(&m_twi, &twi_max30102_config, NULL, NULL); /*BLOCK MODE*/
    APP_ERROR_CHECK(err_code);

    nrf_drv_twi_enable(&m_twi);

    return err_code;
}

bool maxim_max30102_data_ready()
{
    if (nrf_gpio_pin_read(MAX30102_INT_PIN))
    {
        return false;
    }

    return true;
}
