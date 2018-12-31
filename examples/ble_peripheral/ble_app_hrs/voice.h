#ifndef VOICE_H
#define VOICE_H

#include "ble.h"

#ifdef __cplusplus
extern "C" {
#endif

#define VOICE_BUSY_STATUS_PIN 7 // busy管脚编号
#define VOICE_SDA_PIN  6 // SDA管脚编号

#define RTC1_TICKS_PER_SECOND 32768 // RTC1每秒振动次数
#define RTC1_TICKS_PER_MS ((int)(RTC1_TICKS_PER_SECOND/1000)) // RTC1每一毫秒振动次数

#define VOICE_SDA_START_LOW_MS 5 // 数据传输启动时，拉低管脚多少毫秒
#define VOICE_SDA_START_LOW_TICKS (RTC1_TICKS_PER_MS*VOICE_SDA_START_LOW_MS) // ticks计数


#define VOICE_SDA_DATA_SHORT_PART ((int)(RTC1_TICKS_PER_MS/2)) // 数据传输时，短
#define VOICE_SDA_DATA_LONG_PART (VOICE_SDA_DATA_SHORT_PART*3) // 数据传输时，长

// init voice GPIOs
uint32_t voice_init();

uint32_t voice_play(uint32_t recordID);

uint32_t voice_stop();

#ifdef __cplusplus
}
#endif

#endif /*VOICE_H*/
