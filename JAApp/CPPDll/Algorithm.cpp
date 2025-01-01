#include "pch.h"
#include <iostream>
#include <algorithm>
using namespace std;

extern "C" __declspec(dllexport) int ApplyCFilter(int* image, int w, int h) {
	// obliczenie ilosc pixeli w calym obrazie
	int pixels = w * h;

	// wartosc o ile rozjasnimy obraz
	int brightness = 50;

	for (int i = 0; i < pixels; i++) {
		int pixel = image[i];
		int a = (pixel >> 24) & 0xFF;
		int r = (pixel >> 16) & 0xFF;
		int g = (pixel >> 8) & 0xFF;
		int b = pixel & 0xFF;

		r = min(r + brightness, 255);
		g = min(g + brightness, 255);
		b = min(b + brightness, 255);

		image[i] = (a << 24) | (r << 16) | (g << 8) | b;
	}
	return 0;
}