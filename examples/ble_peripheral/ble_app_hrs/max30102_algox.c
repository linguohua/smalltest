#include "max30102_algox.h"

void move_average(int window, const float* ptrInput, int32_t length, float* ptrOutput)
{
    float sum = 0;
    int halfWindow = (int)window / 2;
    int feed = 0;
    int windowSize = 0;
    int index;

    for (int i = 0; i < (1 + halfWindow) && feed < length; i++)
    {
        sum += ptrInput[feed];
        feed++;
        windowSize++;
    }

    for (index = 0; index < (halfWindow) && feed < length;)
    {
        ptrOutput[index] = sum / (float)windowSize;

        sum += ptrInput[feed];

        feed++;
        index++;
        windowSize++;
    }

    for (; feed < length;)
    {
        ptrOutput[index] = sum / (float)windowSize;

        sum += ptrInput[feed];
        sum -= ptrInput[feed - windowSize];

        feed++;
        index++;
    }

    for (; index < length;)
    {
        ptrOutput[index] = sum / (float)windowSize;

        windowSize--;
        sum -= ptrInput[length - windowSize];
        index++;
    }
}

void array_sub(const float* ptrSrc1, int32_t length, const float* ptrSrc2, float* ptrOutput)
{
  for(int i = 0; i < length; i++)
  {
      *ptrOutput = *ptrSrc1 - *ptrSrc2;
      ptrOutput++;
      ptrSrc1++;
      ptrSrc2++;
  }
}

void array_copy(const float* ptrSrc, int32_t length, float* ptrDst)
{
  for(int i = 0; i < length; i++)
  {
      *ptrDst = *ptrSrc;

      ptrSrc++;
      ptrDst++;
  }
}
