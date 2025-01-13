.DATA
    align 16
; wektor [0.04, 0.04, 0.04, 0.04] (1/(5)^2 )
const_0_04  dd 0.04, 0.04, 0.04, 0.04  

.CODE
PUBLIC ApplyASMFilter
ApplyASMFilter PROC
    ;--------------------
    ; rejestry nieulotne  
    ;--------------------
    push    rbp
    push    rbx
    push    rsi
    push    rdi
    push    r12
    push    r13
    push    r14
    push    r15

    ;--------------------
    ; Pobranie parametrów
    ;--------------------
    mov     r10d, [rsp + 104]   ; r10d = imageHeight 
    mov     r12d, r8d           ; r12d = startY
    mov     r13d, edx           ; r13d = width
    mov     rbp, rcx            ; rbp = pixelData
    mov     r9d, r9d            ; r9d = endY

    ;-------------------
    ; Pêtla po wierszach
    ;-------------------
row_loop:
    cmp     r12d, r9d
    jge     end_function        ; Je¿eli y >= endY, koniec

    xor     r14d, r14d         ; x = 0 na start

col_loop:
    cmp     r14d, r13d
    jge     next_row           ; Je¿eli x >= width, przechodzimy do nastêpnego wiersza

    ; Wyzeruj akumulator sumy w xmm0
    pxor    xmm0, xmm0         ; = [0, 0, 0, 0]

    ;---------------------------------------
    ; Pêtla 5x5: dodawanie do xmm0 pikseli z otoczenia
    ;---------------------------------------
    mov     r15d, -2           ; r15d = offset w pionie od aktualnego wiersza

outer_5x5_loop:
    ; SprawdŸ, czy wiersz jest w granicach
    mov     edx, r12d
    add     edx, r15d
    cmp     edx, 0
    jl      skip_row
    cmp     edx, r10d
    jge     skip_row

    ; Inicjalizacja offsetu w poziomie
    mov     r8d, -2

inner_5x5_loop:
    ; SprawdŸ, czy kolumna jest w granicach
    mov     eax, r14d
    add     eax, r8d
    cmp     eax, 0
    jl      skip_col
    cmp     eax, r13d
    jge     skip_col

    ; Oblicz adres danego piksela
    ; rowIndex = (y + r15d) * width
    ; colIndex = (x + r8d)
    ; offset   = (rowIndex + colIndex)*3
    mov     ecx, edx           ; ecx = (y + offsetY)
    imul    ecx, r13d
    add     ecx, eax           ; ecx = (y + offsetY)*width + (x + offsetX)
    imul    ecx, 3             ; bajty/piksel (B,G,R)

    ; Wczytujemy piksel (32 bity) do xmm4
    movd    xmm4, dword ptr [rbp + rcx]

    ; Rozszerzamy 8-bit ? 16-bit ? 32-bit
    pxor    xmm5, xmm5
    punpcklbw xmm4, xmm5       ; 8-bit -> 16-bit
    punpcklwd xmm4, xmm5       ; 16-bit -> 32-bit

    ; Konwertujemy do float i dodajemy do sumy
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

    ;---------------------------------------
    ; Obliczenie œredniej (podzielenie przez 25)
    ; mno¿¹c przez 0.04
    ;---------------------------------------
    mulps   xmm0, xmmword ptr [const_0_04]

    ;---------------------------------------
    ; Konwersja float ? int oraz saturacja
    ;---------------------------------------
    cvttps2dq xmm1, xmm0       ; do liczby ca³kowitej
    packusdw  xmm1, xmm1       ; zbicie do 16 bitów
    packuswb  xmm1, xmm1       ; zbicie do 8 bitów

    ; W xmm1 mamy (w dolnych 32 bitach) 00RRGGBB
    movd    ebx, xmm1

    ;---------------------------------------
    ; Wyliczamy docelowy offset (y*width + x)*3
    ;---------------------------------------
    mov     eax, r12d
    imul    eax, r13d
    add     eax, r14d
    imul    eax, 3

    ; Zapisujemy 4 bajty (B, G, R, X) w pamiêci
    mov     dword ptr [rbp + rax], ebx

    ; Nastêpna kolumna
    inc     r14d
    jmp     col_loop

next_row:
    inc     r12d
    jmp     row_loop

end_function:
    ;-------------------
    ; rejestry nieulotne
    ;-------------------
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