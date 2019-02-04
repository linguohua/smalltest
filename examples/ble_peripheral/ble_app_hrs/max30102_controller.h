#ifndef MAX30102_CONTROLLER_H
#define MAX30102_CONTROLLER_H

#include "app_util_platform.h"
#include "ble.h"
#include "stdbool.h"

#ifdef __cplusplus
extern "C" {
#endif

#define SAMPLE_RATE (100.0f)
#define RAW_DATA_BUFFER_SIZE 512

#define RTC1_TICKS_PER_SECOND 32768 //
#define RTC1_TICKS_PER_MS ((int)(RTC1_TICKS_PER_SECOND/1000)) //

#define FIFO_READ_PERIOD_MS 50 //
#define FIFO_READ_PERIOD_TICKS (RTC1_TICKS_PER_MS*FIFO_READ_PERIOD_MS) //

void feed_red_ired(float red, float ired);

void max30102_sensor_init(void);

void max30102_sensor_start(void);

void max30102_sensor_stop(void);

#ifdef __cplusplus
}
#endif

#endif /*MAX30102_CONTROLLER_H*/
