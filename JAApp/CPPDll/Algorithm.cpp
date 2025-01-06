#include "pch.h"
#include <vector>
#include <algorithm>

// Funkcja clamp - ogranicza warto�� do zadanego zakresu (minValue, maxValue) wbudowana funckaj w bibliotece algorithm nie dzialala 
inline float clamp(float value, float minValue, float maxValue) {
    if (value < minValue) return minValue;
    if (value > maxValue) return maxValue;
    return value;
}

extern "C" __declspec(dllexport) int ApplyCFilter(unsigned char* pixelData, int width, int startY, int endY, int imageHeight) {
	//wielkosc maski
    const int maskSize = 5;
	//wyznacznie po�owy maski w celu ustaleniu zakresu s�siedztwa
    const int halfMask = maskSize / 2;
	//warto�� maski
    const float maskValue = 1.0f / 25.0f;

	//iterowanie po wierszach i kolumnach obrazu
    for (int y = startY; y < endY; ++y) {
        for (int x = 0; x < width; ++x) {
			//inicjowanie zmiennych sumuj�cych warto�ci kolor�w
            float sumBlue = 0.0f, sumGreen = 0.0f, sumRed = 0.0f;
            
			//iterowanie po masce
            for (int dy = -halfMask; dy <= halfMask; ++dy) {
                for (int dx = -halfMask; dx <= halfMask; ++dx) {
					//wyznaczenie wsp�rz�dnych piksela s�siedniego
                    int nx = x + dx;
                    int ny = y + dy;
					//sprawdzenie czy piksel s�siedni znajduje si� w zakresie obrazu
                    if (nx >= 0 && nx < width && ny >= 0 && ny < imageHeight) {
						//indeksowanie piksela s�siedniego
                        int index = (ny * width + nx) * 3;
                        sumBlue += pixelData[index] * maskValue;
                        sumGreen += pixelData[index + 1] * maskValue;
                        sumRed += pixelData[index + 2] * maskValue;
                    }
                }
            }

            int index = (y * width + x) * 3;
			//sprawdzenie czy warto�� nie przekracza znajduj� si� w zakresie 0-255 - taki jest zakres warto�ci kolor�w
            pixelData[index] = static_cast<unsigned char>(clamp(sumBlue, 0.0f, 255.0f));
            pixelData[index + 1] = static_cast<unsigned char>(clamp(sumGreen, 0.0f, 255.0f));
            pixelData[index + 2] = static_cast<unsigned char>(clamp(sumRed, 0.0f, 255.0f));
        }
    }
    return 0;
}
