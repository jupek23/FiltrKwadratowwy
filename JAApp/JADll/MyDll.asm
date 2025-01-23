.DATA
    align 16
; -------------------------------------------
; Sta³a `const_0_04`:
; - Opis: Wektor o wartoœciach [0.04, 0.04, 0.04, 0.04].
;   Wartoœci te odpowiadaj¹ 1/25, co jest wspó³czynnikiem dla filtru 5x5.
; - U¿ycie: Umo¿liwia obliczanie œredniej wartoœci pikseli w oknie 5x5
;   przez mno¿enie akumulatora sumy przez tê sta³¹.
; - Rozmiar: 16 bajtów (4 wartoœci float, ka¿da 4 bajty).
; -------------------------------------------
const_0_04  dd 0.04, 0.04, 0.04, 0.04  

.CODE
PUBLIC ApplyASMFilter
; -------------------------------------------
; Funkcja `ApplyASMFilter`:
; - Opis: Przetwarza obraz w pamiêci, stosuj¹c filtr 5x5 (œrednia wartoœci).
;   Obraz wejœciowy i wyjœciowy znajduj¹ siê w tej samej pamiêci.
; - Parametry wejœciowe:
;   * rcx: wskaŸnik na dane pikseli (pixelData) - tablica bajtów RGB.
;   * rdx: szerokoœæ obrazu (width) - liczba pikseli w wierszu (>0).
;   * r8: pocz¹tkowy wiersz do przetwarzania (startY, >=0).
;   * r9: koñcowy wiersz do przetwarzania (endY, <= wysokoœæ obrazu).
;   * [rsp+104]: wysokoœæ obrazu (imageHeight) - liczba wierszy (>0).
; - Parametry wyjœciowe:
;   * Przetworzony obraz zapisany w pamiêci (pixelData).
; - Rejestry modyfikowane:
;   * Zmienne: xmm0, xmm1, xmm4, xmm5, rax, rbx, rcx, rdx, r8, r9, r10, r12, r13, r14, r15.
;   * Rejestry nieulotne: rbp, rbx, rsi, rdi, r12, r13, r14, r15 (przywracane przed zakoñczeniem).
; -------------------------------------------
ApplyASMFilter PROC
    ; -------------------------------------------
    ; Zabezpieczenie rejestrów nieulotnych przed zmian¹.
    ; Rejestry: rbp, rbx, rsi, rdi, r12, r13, r14, r15.
    ; -------------------------------------------
    push    rbp
    push    rbx
    push    rsi
    push    rdi
    push    r12
    push    r13
    push    r14
    push    r15

    ; -------------------------------------------
    ; Pobranie parametrów wejœciowych:
    ; - r10d: wysokoœæ obrazu (imageHeight).
    ; - r12d: pocz¹tkowy wiersz (startY).
    ; - r13d: szerokoœæ obrazu (width).
    ; - rbp: wskaŸnik na dane pikseli (pixelData).
    ; - r9d: koñcowy wiersz (endY).
    ; -------------------------------------------
    mov     r10d, [rsp + 104]   ; Wczytanie wysokoœci obrazu do r10d.
    mov     r12d, r8d           ; Ustawienie startY w r12d.
    mov     r13d, edx           ; Szerokoœæ obrazu (width) do r13d.
    mov     rbp, rcx            ; WskaŸnik na dane pikseli (pixelData) do rbp.
    mov     r9d, r9d            ; Koñcowy wiersz (endY) do r9d.

    ; -------------------------------------------
    ; Pêtla wierszy (row_loop):
    ; - Iteruje od startY (r12d) do endY (r9d).
    ; - Przetwarza kolejne wiersze obrazu.
    ; -------------------------------------------
row_loop:
    cmp     r12d, r9d
    jge     end_function        ; Jeœli r12d >= r9d, zakoñcz przetwarzanie.

    xor     r14d, r14d          ; Ustawienie kolumny x = 0 (r14d).

    ; -------------------------------------------
    ; Pêtla kolumn (col_loop):
    ; - Iteruje od x = 0 do x = width.
    ; - Przetwarza kolejne piksele w bie¿¹cym wierszu.
    ; -------------------------------------------
col_loop:
    cmp     r14d, r13d
    jge     next_row           ; Jeœli x >= width, przejdŸ do kolejnego wiersza.

    ; Wyzerowanie akumulatora sumy w rejestrze xmm0.
    pxor    xmm0, xmm0         ; xmm0 = [0, 0, 0, 0].

    ; -------------------------------------------
    ; Pêtla przetwarzania okna 5x5 (outer_5x5_loop):
    ; - Iteruje po wierszach od -2 do +2 wokó³ bie¿¹cego piksela.
    ; -------------------------------------------
    mov     r15d, -2           ; Ustawienie offsetu pionowego (-2).

outer_5x5_loop:
    ; Sprawdzenie, czy bie¿¹cy wiersz mieœci siê w granicach obrazu.
    mov     edx, r12d
    add     edx, r15d          ; edx = bie¿¹cy wiersz + offset.
    cmp     edx, 0
    jl      skip_row           ; Jeœli wiersz < 0, pomiñ.
    cmp     edx, r10d
    jge     skip_row           ; Jeœli wiersz >= wysokoœæ obrazu, pomiñ.

    ; Inicjalizacja offsetu poziomego (-2).
    mov     r8d, -2

inner_5x5_loop:
    ; Sprawdzenie, czy bie¿¹ca kolumna mieœci siê w granicach obrazu.
    mov     eax, r14d
    add     eax, r8d           ; eax = bie¿¹ca kolumna + offset.
    cmp     eax, 0
    jl      skip_col           ; Jeœli kolumna < 0, pomiñ.
    cmp     eax, r13d
    jge     skip_col           ; Jeœli kolumna >= szerokoœæ obrazu, pomiñ.

    ; Obliczanie adresu bie¿¹cego piksela:
    ; - rowIndex = (y + offsetY) * width.
    ; - colIndex = x + offsetX.
    mov     ecx, edx           ; ecx = y + offsetY.
    imul    ecx, r13d          ; ecx = (y + offsetY) * width.
    add     ecx, eax           ; ecx = (y + offsetY) * width + (x + offsetX).
    imul    ecx, 3             ; Skalowanie adresu (3 bajty na piksel).

    ; Wczytanie bie¿¹cego piksela (B, G, R) do xmm4.
    movd    xmm4, dword ptr [rbp + rcx]

    ; Rozszerzenie wartoœci pikseli (8-bit -> 32-bit).
    pxor    xmm5, xmm5         ; Zerowanie xmm5.
    punpcklbw xmm4, xmm5       ; Rozszerzenie do 16-bit.
    punpcklwd xmm4, xmm5       ; Rozszerzenie do 32-bit.

    ; Konwersja wartoœci do float i dodanie do akumulatora sumy.
    cvtdq2ps xmm4, xmm4
    addps   xmm0, xmm4

skip_col:
    add     r8d, 1
    cmp     r8d, 2
    jle     inner_5x5_loop

skip_row:
    add     r15d, 1
    cmp     r15d, 2
    jle     outer_5x5_loop

    ; -------------------------------------------
    ; Obliczanie œredniej pikseli w oknie 5x5.
    ; -------------------------------------------
    mulps   xmm0, xmmword ptr [const_0_04]

    ; -------------------------------------------
    ; Konwersja wartoœci float -> int.
    ; Saturacja wartoœci do zakresu [0, 255].
    ; -------------------------------------------
    cvttps2dq xmm1, xmm0       ; Konwersja do liczb ca³kowitych.
    packusdw  xmm1, xmm1       ; Pakowanie do 16-bit.
    packuswb  xmm1, xmm1       ; Pakowanie do 8-bit.

    ; Zapisanie przetworzonego piksela.
    movd    ebx, xmm1          ; Wynik w ebx (00RRGGBB).
    mov     eax, r12d          ; eax = y (startY).
    imul    eax, r13d          ; eax = y * width.
    add     eax, r14d          ; eax = y * width + x.
    imul    eax, 3             ; Skalowanie adresu (3 bajty na piksel).
    mov     dword ptr [rbp + rax], ebx

    ; Nastêpna kolumna.
    inc     r14d
    jmp     col_loop

next_row:
    ; Nastêpny wiersz.
    inc     r12d
    jmp     row_loop

end_function:
    ; -------------------------------------------
    ; Przywracanie rejestrów nieulotnych.
    ; -------------------------------------------
    pop     r15
    pop     r14
    pop     r13
    pop     r12
    pop     rdi
    pop     rsi
    pop     rbx
    pop     rbp
    ret
ApplyASMFilter ENDP
END
