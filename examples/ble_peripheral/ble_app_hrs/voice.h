#ifndef VOICE_H
#define VOICE_H

#include "ble.h"

#ifdef __cplusplus
extern "C" {
#endif

#define VOICE_BUSY_STATUS_PIN 7 //
#define VOICE_SDA_PIN  6 //

#define RTC1_TICKS_PER_SECOND 32768 //
#define RTC1_TICKS_PER_MS ((int)(RTC1_TICKS_PER_SECOND/1000)) //

#define VOICE_SDA_START_LOW_MS 5 //
#define VOICE_SDA_START_LOW_TICKS (RTC1_TICKS_PER_MS*VOICE_SDA_START_LOW_MS) //


#define VOICE_SDA_DATA_SHORT_PART ((int)(RTC1_TICKS_PER_MS/2)) //
#define VOICE_SDA_DATA_LONG_PART (VOICE_SDA_DATA_SHORT_PART*3) //

// init voice GPIOs
uint32_t voice_init();

uint32_t voice_play(unsigned char* ptr_bytes, unsigned char length);

uint32_t voice_stop();

#ifdef __cplusplus
}
#endif

#endif /*VOICE_H*/
