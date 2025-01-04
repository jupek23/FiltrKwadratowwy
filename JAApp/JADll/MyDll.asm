.DATA
    align 16
const_0_04  dd 0.04, 0.04, 0.04, 0.04  ; wektor [0.04, 0.04, 0.04, 0.04]

.CODE
PUBLIC ApplyASMFilter
ApplyASMFilter PROC
    ; Zapisz rejestry nieulotne
    push rbp
    push rbx
    push rsi
    push rdi
    push r12
    push r13
    push r14
    push r15

    ; Pobierz parametry
    mov r10d, [rsp + 104]    ; r10d = imageHeight
    mov r12d, r8d            ; r12d = startY
    mov r13d, edx            ; r13d = width
    mov rbp, rcx             ; rbp = pixelData (wska�nik do danych pikseli)
    mov r9d, r9d             ; r9d = endY

    ; P�tla po wierszach
row_loop:
    cmp r12d, r9d
    jge end_function         ; Je�eli y >= endY, zako�cz

    ; Inicjalizacja x
    xor r14d, r14d           ; r14d = x

col_loop:
    cmp r14d, r13d
    jge next_row             ; Je�eli x >= width, przejd� do nast�pnego wiersza

    ; Inicjalizacja sum�w pikseli
    pxor xmm0, xmm0          ; xmm0 = [0,0,0,0] - b�dzie sumowa� [B,G,R,A?]

    ; R�czne sumowanie pikseli w s�siedztwie 5x5
    mov ecx, r12d
    sub ecx, 2               ; y-2
    call process_row
    add ecx, 1               ; y-1
    call process_row
    add ecx, 1               ; y
    call process_row
    add ecx, 1               ; y+1
    call process_row
    add ecx, 1               ; y+2
    call process_row

    ; Podziel sumy przez 25 (mno��c przez 0.04)
    mulps xmm0, xmmword ptr [const_0_04]

    ; Konwertuj z powrotem do liczb ca�kowitych
    cvttps2dq xmm1, xmm0      ; w xmm1 mamy [B_int, G_int, R_int, A_int?]

    ; Ogranicz do zakresu 0-255
    packusdw xmm1, xmm1       ; Sk�adanie do 16-bitowych liczb
    packuswb xmm1, xmm1       ; Sk�adanie do 8-bitowych liczb

    ; Przekszta�� do formatu 00RRGGBB
    movd ebx, xmm1            ; Zapisz dolne 32 bity (00RRGGBB) do rejestru ebx

    ; Oblicz offset docelowy w pami�ci
    mov eax, r12d
    imul eax, r13d
    add eax, r14d
    imul eax, 3

    ; Zapisz 4 bajty (B, G, R, X) do pami�ci
    mov dword ptr [rbp + rax], ebx

    inc r14d
    jmp col_loop

next_row:
    inc r12d
    jmp row_loop

end_function:
    ; Przywr�� rejestry
    pop r15
    pop r14
    pop r13
    pop r12
    pop rdi
    pop rsi
    pop rbx
    pop rbp
    ret

; ------------------------------------------------------
; process_row: Dodaje do xmm0 piksele z wiersza ecx
;              w kolumnach [x-2..x+2], je�li mieszcz� si� w obrazie
; ------------------------------------------------------
process_row PROC
    cmp ecx, 0
    jl skip_row
    cmp ecx, r10d
    jge skip_row

    mov esi, ecx
    imul esi, r13d
    add esi, r14d
    imul esi, 3

    mov r15d, -2
sum_x_offsets:
    mov eax, r14d
    add eax, r15d
    cmp eax, 0
    jl skip_x_offset
    cmp eax, r13d
    jge skip_x_offset

    mov edx, ecx
    imul edx, r13d
    add edx, eax
    imul edx, 3
    mov esi, edx
    call sum_pixels_row

skip_x_offset:
    add r15d, 1
    cmp r15d, 2
    jle sum_x_offsets

skip_row:
    ret
process_row ENDP

; ------------------------------------------------------
; sum_pixels_row: Wczytuje piksel (B,G,R + 1 bajt "nadmiarowy")
;                 i dodaje go do xmm0 w formacie float: [B, G, R, A?]
; ------------------------------------------------------
sum_pixels_row PROC
    ; Wczytujemy 4 bajty (B, G, R, X) do rejestru XMM4.
    movd xmm4, dword ptr [rbp + rsi]     ; wczytaj 32 bity

    ; Przygotowujemy XMM5 = 0, by �roszerza� bajty do word/dword.
    pxor xmm5, xmm5

    ; Rozszerz z 8-bit�w do 16-bit�w.
    punpcklbw xmm4, xmm5                 ; po tym xmm4 = [B, G, R, A?] w 16-bitach

    ; Rozszerz z 16-bit�w do 32-bit�w.
    punpcklwd xmm4, xmm5                 ; po tym xmm4 = [B, G, R, A?] w 32-bitach

    ; Konwersja z 32-bit�w do float.
    cvtdq2ps xmm4, xmm4                  ; xmm4 = [B_float, G_float, R_float, A_float?]

    ; Dodaj do sumy w xmm0
    addps xmm0, xmm4

    ret
sum_pixels_row ENDP

ApplyASMFilter ENDP
END