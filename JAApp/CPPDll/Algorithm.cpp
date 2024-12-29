#include "pch.h"

extern "C" __declspec(dllexport) int ApplyCFilter(int a, int b) {
    return a + b;
}