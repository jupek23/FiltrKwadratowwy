.DATA
    align 16
; -------------------------------------------
; Sta�a `const_0_04`:
; - Opis: Wektor o warto�ciach [0.04, 0.04, 0.04, 0.04].
;   Warto�ci te odpowiadaj� 1/25, co jest wsp�czynnikiem dla filtru 5x5.
; - U�ycie: Umo�liwia obliczanie �redniej warto�ci pikseli w oknie 5x5
;   przez mno�enie akumulatora sumy przez t� sta��.
; - Rozmiar: 16 bajt�w (4 warto�ci float, ka�da 4 bajty).
; -------------------------------------------
const_0_04  dd 0.04, 0.04, 0.04, 0.04  

.CODE
PUBLIC ApplyASMFilter
; -------------------------------------------
; Funkcja `ApplyASMFilter`:
; - Opis: Przetwarza obraz w pami�ci, stosuj�c filtr 5x5 (�rednia warto�ci).
;   Obraz wej�ciowy i wyj�ciowy znajduj� si� w tej samej pami�ci.
; - Parametry wej�ciowe:
;   * rcx: wska�nik na dane pikseli (pixelData) - tablica bajt�w RGB.
;   * rdx: szeroko�� obrazu (width) - liczba pikseli w wierszu (>0).
;   * r8: pocz�tkowy wiersz do przetwarzania (startY, >=0).
;   * r9: ko�cowy wiersz do przetwarzania (endY, <= wysoko�� obrazu).
;   * [rsp+104]: wysoko�� obrazu (imageHeight) - liczba wierszy (>0).
; - Parametry wyj�ciowe:
;   * Przetworzony obraz zapisany w pami�ci (pixelData).
; - Rejestry modyfikowane:
;   * Zmienne: xmm0, xmm1, xmm4, xmm5, rax, rbx, rcx, rdx, r8, r9, r10, r12, r13, r14, r15.
;   * Rejestry nieulotne: rbp, rbx, rsi, rdi, r12, r13, r14, r15 (przywracane przed zako�czeniem).
; -------------------------------------------
ApplyASMFilter PROC
    ; -------------------------------------------
    ; Zabezpieczenie rejestr�w nieulotnych przed zmian�.
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
    ; Pobranie parametr�w wej�ciowych:
    ; - r10d: wysoko�� obrazu (imageHeight).
    ; - r12d: pocz�tkowy wiersz (startY).
    ; - r13d: szeroko�� obrazu (width).
    ; - rbp: wska�nik na dane pikseli (pixelData).
    ; - r9d: ko�cowy wiersz (endY).
    ; -------------------------------------------
    mov     r10d, [rsp + 104]   ; Wczytanie wysoko�ci obrazu do r10d.
    mov     r12d, r8d           ; Ustawienie startY w r12d.
    mov     r13d, edx           ; Szeroko�� obrazu (width) do r13d.
    mov     rbp, rcx            ; Wska�nik na dane pikseli (pixelData) do rbp.
    mov     r9d, r9d            ; Ko�cowy wiersz (endY) do r9d.

    ; -------------------------------------------
    ; P�tla wierszy (row_loop):
    ; - Iteruje od startY (r12d) do endY (r9d).
    ; - Przetwarza kolejne wiersze obrazu.
    ; -------------------------------------------
row_loop:
    cmp     r12d, r9d
    jge     end_function        ; Je�li r12d >= r9d, zako�cz przetwarzanie.

    xor     r14d, r14d          ; Ustawienie kolumny x = 0 (r14d).

    ; -------------------------------------------
    ; P�tla kolumn (col_loop):
    ; - Iteruje od x = 0 do x = width.
    ; - Przetwarza kolejne piksele w bie��cym wierszu.
    ; -------------------------------------------
col_loop:
    cmp     r14d, r13d
    jge     next_row           ; Je�li x >= width, przejd� do kolejnego wiersza.

    ; Wyzerowanie akumulatora sumy w rejestrze xmm0.
    pxor    xmm0, xmm0         ; xmm0 = [0, 0, 0, 0].

    ; -------------------------------------------
    ; P�tla przetwarzania okna 5x5 (outer_5x5_loop):
    ; - Iteruje po wierszach od -2 do +2 wok� bie��cego piksela.
    ; -------------------------------------------
    mov     r15d, -2           ; Ustawienie offsetu pionowego (-2).

outer_5x5_loop:
    ; Sprawdzenie, czy bie��cy wiersz mie�ci si� w granicach obrazu.
    mov     edx, r12d
    add     edx, r15d          ; edx = bie��cy wiersz + offset.
    cmp     edx, 0
    jl      skip_row           ; Je�li wiersz < 0, pomi�.
    cmp     edx, r10d
    jge     skip_row           ; Je�li wiersz >= wysoko�� obrazu, pomi�.

    ; Inicjalizacja offsetu poziomego (-2).
    mov     r8d, -2

inner_5x5_loop:
    ; Sprawdzenie, czy bie��ca kolumna mie�ci si� w granicach obrazu.
    mov     eax, r14d
    add     eax, r8d           ; eax = bie��ca kolumna + offset.
    cmp     eax, 0
    jl      skip_col           ; Je�li kolumna < 0, pomi�.
    cmp     eax, r13d
    jge     skip_col           ; Je�li kolumna >= szeroko�� obrazu, pomi�.

    ; Obliczanie adresu bie��cego piksela:
    ; - rowIndex = (y + offsetY) * width.
    ; - colIndex = x + offsetX.
    mov     ecx, edx           ; ecx = y + offsetY.
    imul    ecx, r13d          ; ecx = (y + offsetY) * width.
    add     ecx, eax           ; ecx = (y + offsetY) * width + (x + offsetX).
    imul    ecx, 3             ; Skalowanie adresu (3 bajty na piksel).

    ; Wczytanie bie��cego piksela (B, G, R) do xmm4.
    movd    xmm4, dword ptr [rbp + rcx]

    ; Rozszerzenie warto�ci pikseli (8-bit -> 32-bit).
    pxor    xmm5, xmm5         ; Zerowanie xmm5.
    punpcklbw xmm4, xmm5       ; Rozszerzenie do 16-bit.
    punpcklwd xmm4, xmm5       ; Rozszerzenie do 32-bit.

    ; Konwersja warto�ci do float i dodanie do akumulatora sumy.
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
    ; Obliczanie �redniej pikseli w oknie 5x5.
    ; -------------------------------------------
    mulps   xmm0, xmmword ptr [const_0_04]

    ; -------------------------------------------
    ; Konwersja warto�ci float -> int.
    ; Saturacja warto�ci do zakresu [0, 255].
    ; -------------------------------------------
    cvttps2dq xmm1, xmm0       ; Konwersja do liczb ca�kowitych.
    packusdw  xmm1, xmm1       ; Pakowanie do 16-bit.
    packuswb  xmm1, xmm1       ; Pakowanie do 8-bit.

    ; Zapisanie przetworzonego piksela.
    movd    ebx, xmm1          ; Wynik w ebx (00RRGGBB).
    mov     eax, r12d          ; eax = y (startY).
    imul    eax, r13d          ; eax = y * width.
    add     eax, r14d          ; eax = y * width + x.
    imul    eax, 3             ; Skalowanie adresu (3 bajty na piksel).
    mov     dword ptr [rbp + rax], ebx

    ; Nast�pna kolumna.
    inc     r14d
    jmp     col_loop

next_row:
    ; Nast�pny wiersz.
    inc     r12d
    jmp     row_loop

end_function:
    ; -------------------------------------------
    ; Przywracanie rejestr�w nieulotnych.
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
