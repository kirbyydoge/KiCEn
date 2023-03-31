using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Runtime.CompilerServices;

// BitOperations.PopCount
// BitOperations.LeadingZeroCount
// BitOperations.TrailingZeroCount

public class BitBoardMoveGenerator {

    private static readonly string EMPTY_POS = "8/8/8/8/8/8/8/8/8/8 - - -";
    private static readonly string START_POS = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private static readonly string EVAL_POS = "r3k2r/p11pqpb1/bn2pnp1/2pPN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq c6 0 1";
    
    // Upper case picese are white, lower case pieces are black
    private enum BitPiece {
        P, N, B, R, Q, K, p, n, b, r, q, k, invalid
    };

    public enum BitColor { 
        WHITE, BLACK, ALL
    };

    public enum BitSquare {
        a8, b8, c8, d8, e8, f8, g8, h8,
        a7, b7, c7, d7, e7, f7, g7, h7,
        a6, b6, c6, d6, e6, f6, g6, h6,
        a5, b5, c5, d5, e5, f5, g5, h5,
        a4, b4, c4, d4, e4, f4, g4, h4,
        a3, b3, c3, d3, e3, f3, g3, h3,
        a2, b2, c2, d2, e2, f2, g2, h2,
        a1, b1, c1, d1, e1, f1, g1, h1, invalid
    };

    private enum BitCastling { 
        WK = 1, WQ = 2, BK = 4, BQ = 8
    };

    // Used for pawns
    private const ulong not_a_file = 18374403900871474942UL;
    private const ulong not_h_file = 9187201950435737471UL;

    // Used for knights
    private const ulong not_hg_file = 4557430888798830399UL;
    private const ulong not_ab_file = 18229723555195321596UL;

    // I hate this (int)Enum.element cast requirement bullshit of C#
    private readonly int COL_WHITE = (int)BitColor.WHITE;
    private readonly int COL_BLACK = (int)BitColor.BLACK;
    private readonly int COL_ALL = (int)BitColor.ALL;

    // Lookup tables
    public ulong[,] pawn_attack_lut;
    public ulong[] knight_attack_lut;
    public ulong[] king_attack_lut;
    public ulong[,] bishop_attack_lut;
    public ulong[,] rook_attack_lut;
    public ulong[] bishop_mask_lut;
    public ulong[] rook_mask_lut;

    public ulong[] diagonal_magic_numbers;
    public ulong[] orthogonal_magic_numbers;
    public int[] diagonal_relevant_bits;
    public int[] orthogonal_relevant_bits;

    private ulong[] bitboards;
    private ulong[] occupancies;
    private BitColor side_to_move;
    private BitSquare en_passant_square;
    private char castling_rights;

    private static readonly int[] multiply_de_bruijn_bit_pos = new int[64] {
        0,  47,  1, 56, 48, 27,  2, 60,
        57, 49, 41, 37, 28, 16,  3, 61,
        54, 58, 35, 52, 50, 42, 21, 44,
        38, 32, 29, 23, 17, 11,  4, 62,
        46, 55, 26, 59, 40, 36, 15, 53,
        34, 51, 20, 43, 31, 22, 10, 45,
        25, 39, 14, 33, 19, 30,  9, 24,
        13, 18,  8, 12,  7,  6,  5, 63
    };

    // PCG Random Number Generator Declarations
    private static uint pcg_state;
    private static uint pcg_increment;
    public BitBoardMoveGenerator() {
        init_pawn_attacks();
        init_knight_attacks();
        init_king_attacks();
        init_magic_numbers(true, false);
        init_diagonal_attacks();
        init_orthogonal_attacks();
        side_to_move = BitColor.WHITE;
        en_passant_square = BitSquare.invalid;
        bitboards = new ulong[12];
        occupancies = new ulong[3];
        castling_rights = (char)(BitCastling.BK | BitCastling.BQ | BitCastling.WK | BitCastling.WQ);
        load_fen(START_POS);
    }

    private static char get_piece_char(BitPiece piece) {
        string lookup = "PNBRQKpnbrqk";
        return lookup[(int)piece];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void set_bitboard(BitPiece piece, BitSquare square) {
        set_bit(ref bitboards[(int)piece], (int)square);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void clear_bitboard(BitPiece piece, BitSquare square) {
        clear_bit(ref bitboards[(int)piece], (int)square);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void set_occupancy(BitColor side, BitSquare square) {
        set_bit(ref bitboards[(int)side], (int)square);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void clear_occupancy(BitColor side, BitSquare square) {
        clear_bit(ref bitboards[(int)side], (int)square);
    }

    private void load_fen(string fen) {
        bitboards = new ulong[12];
        occupancies = new ulong[3];
        side_to_move = BitColor.ALL;
        en_passant_square = BitSquare.invalid;
        castling_rights = (char)0;
        int rank = 0;
        int file = 0;
        string[] parts = fen.Split(' ');
        string position = parts[0];
        string to_play = parts[1];
        string castling = parts[2];
        string en_passant = parts[3];
        for (int i = 0; i < position.Length; i++) {
            char cmd = position[i];
            if (cmd == '/') {
                file = 0; rank++;
            }
            else if (char.IsDigit(cmd)) {
                file += cmd - '0';
            }
            else {
                BitPiece piece = (BitPiece)System.Enum.Parse(typeof(BitPiece), cmd + "");
                set_bitboard(piece, (BitSquare)(rank * 8 + file));
                file++;
            }
        }
        if (to_play[0] == 'w') side_to_move = BitColor.WHITE;
        else side_to_move = BitColor.BLACK;
        for (int i = 0; i < castling.Length; i++) {
            switch (castling[i]) {
                case 'K':
                    castling_rights |= (char)BitCastling.WK;
                    break;
                case 'Q':
                    castling_rights |= (char)BitCastling.WQ;
                    break;
                case 'k':
                    castling_rights |= (char)BitCastling.BK;
                    break;
                case 'q':
                    castling_rights |= (char)BitCastling.BQ;
                    break;
            }
        }
        if (en_passant[0] != '-') {
            int ep_file = en_passant[0] - 'a';
            int ep_rank = '8' - en_passant[1];
            en_passant_square = (BitSquare)(ep_rank * 8 + ep_file);
        }
        for (BitPiece piece = BitPiece.P; piece < BitPiece.p; piece++) {
            occupancies[(int)BitColor.WHITE] |= bitboards[(int)piece];
        }
        for (BitPiece piece = BitPiece.p; piece < BitPiece.invalid; piece++) {
            occupancies[(int)BitColor.BLACK] |= bitboards[(int)piece];
        }
        occupancies[(int)BitColor.ALL] = occupancies[(int)BitColor.WHITE] | occupancies[(int)BitColor.BLACK];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int get_lsb_index(ulong value) {
        if (value == 0) {
            return -1;
        }
        ulong debruijn64 = 0x03f79d71b4cb0a89UL;
        return multiply_de_bruijn_bit_pos[((value ^ (value-1)) * debruijn64) >> 58];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int pop_lsb(ref ulong number) {
        int index = get_lsb_index(number);
        clear_bit(ref number, index);
        return index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int pop_count(ulong number) {
        const ulong m1 = 0x5555555555555555UL;
        const ulong m2 = 0x3333333333333333UL;
        const ulong m4 = 0x0f0f0f0f0f0f0f0fUL;
        const ulong h01 = 0x0101010101010101UL;

        number -= (number >> 1) & m1;
        number = (number & m2) + ((number >> 2) & m2);
        number = (number + (number >> 4)) & m4;
        return (int)((number * h01) >> 56);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void set_bit(ref ulong number, int idx) {
        number |= 1UL << idx;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool get_bit(ulong number, int idx) {
        return (number &= 1UL << idx) > 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void clear_bit(ref ulong number, int idx) {
        number &= ~(1UL << idx);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int get_magic_index(ulong occupancy, ulong magic_number, int relevant_bits) {
        return (int)((occupancy * magic_number) >> (64 - relevant_bits));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong get_bishop_attacks(int square, ulong occupancy) {
        ulong magic_occupancy = (bishop_mask_lut[square] & occupancy) * diagonal_magic_numbers[square];
        return bishop_attack_lut[square, magic_occupancy >> (64 - diagonal_relevant_bits[square])];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong get_rook_attacks(int square, ulong occupancy) {
        ulong magic_occupancy = (rook_mask_lut[square] & occupancy) * orthogonal_magic_numbers[square];
        return rook_attack_lut[square, magic_occupancy >> (64 - orthogonal_relevant_bits[square])];
    }

    // You could also call the functions above and OR them, but this way we enforce inlining ourselves.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ulong get_queen_attacks(int square, ulong occupancy) {
        ulong bishop_occupancy = (bishop_mask_lut[square] & occupancy) * diagonal_magic_numbers[square];
        ulong bishop_attacks = bishop_attack_lut[square, bishop_occupancy >> (64 - diagonal_relevant_bits[square])];
        ulong rook_occupancy = (rook_mask_lut[square] & occupancy) * orthogonal_magic_numbers[square];
        ulong rook_attacks = rook_attack_lut[square, rook_occupancy >> (64 - orthogonal_relevant_bits[square])];
        return bishop_attacks | rook_attacks;
    }

    private static ulong bit_pawn_attack(int square, int side) {
        ulong bitboard = 0UL;
        ulong attacks = 0UL;
        set_bit(ref bitboard, square);
        if (side == (int) BitColor.WHITE) {
            if ((bitboard & not_a_file) > 0) {
                attacks |= bitboard >> 9;
            }
            if ((bitboard & not_h_file) > 0) {
                attacks |= bitboard >> 7;
            }
        }
        else {
            if ((bitboard & not_a_file) > 0) {
                attacks |= bitboard << 7;
            }
            if ((bitboard & not_h_file) > 0) {
                attacks |= bitboard << 9;
            }
        }
        return attacks;
    }

    private static ulong bit_knight_attack(int square) {
        ulong bitboard = 0UL;
        ulong attacks = 0UL;
        set_bit(ref bitboard, square);
        // 2 up 1 left
        if (((bitboard >> 17) & not_h_file) > 0) {
            attacks |= bitboard >> 17;
        }
        // 2 up 1 right
        if (((bitboard >> 15) & not_a_file) > 0) {
            attacks |= bitboard >> 15;
        }
        // 1 up 2 left 
        if (((bitboard >> 10) & not_hg_file) > 0) {
            attacks |= bitboard >> 10;
        }
        // 1 up 2 right 
        if (((bitboard >> 6) & not_ab_file) > 0) {
            attacks |= bitboard >> 6;
        }
        // 2 down 1 left
        if (((bitboard << 17) & not_a_file) > 0) {
            attacks |= bitboard << 17;
        }
        // 2 down 1 right
        if (((bitboard << 15) & not_h_file) > 0) {
            attacks |= bitboard << 15;
        }
        // 1 down 2 left 
        if (((bitboard << 10) & not_ab_file) > 0) {
            attacks |= bitboard << 10;
        }
        // 1 down 2 right 
        if (((bitboard << 6) & not_hg_file) > 0) {
            attacks |= bitboard << 6;
        }
        return attacks;
    }

    private static ulong bit_king_attack(int square) {
        ulong bitboard = 0UL;
        ulong attacks = 0UL;
        set_bit(ref bitboard, square);
        attacks |= bitboard << 8; // Up
        attacks |= bitboard >> 8; // Down
        if ((bitboard & not_a_file) > 0) {
            attacks |= bitboard >> 1; // Left
            attacks |= bitboard >> 9; // Up Left
            attacks |= bitboard << 7; // Down Left
        }
        if ((bitboard & not_h_file) > 0) {
            attacks |= bitboard << 1; // Right
            attacks |= bitboard >> 7; // Up Right
            attacks |= bitboard << 9; // Down Right
        }
        return attacks;
    }

    private static ulong bit_diagonal_attack(int square) {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;
        int[,] directions = {
            {1, 1},
            {1, -1},
            {-1, 1},
            {-1, -1}
        };
        for(int i = 0; i < directions.GetLength(0); i++) {
            int r_idx = rank + directions[i, 0];
            int f_idx = file + directions[i, 1];
            while (r_idx <= 6 && f_idx <= 6 && r_idx >= 1 && f_idx >= 1) {
                attacks |= 1UL << (r_idx * 8 + f_idx);
                r_idx += directions[i, 0]; f_idx += directions[i, 1];
            }
        }
        return attacks;
    }

    private static ulong bit_diagonal_attack_blocked(int square, ulong occupancy) {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;
        int[,] directions = {
            {1, 1},
            {1, -1},
            {-1, 1},
            {-1, -1}
        };
        for (int i = 0; i < directions.GetLength(0); i++) {
            int r_idx = rank + directions[i, 0];
            int f_idx = file + directions[i, 1];
            while (r_idx <= 7 && f_idx <= 7 && r_idx >= 0 && f_idx >= 0) {
                attacks |= 1UL << (r_idx * 8 + f_idx);
                if (get_bit(occupancy, (r_idx * 8 + f_idx))) {
                    break;
                }
                r_idx += directions[i, 0]; f_idx += directions[i, 1];
            }
        }
        return attacks;
    }

    private static ulong bit_orthogonal_attack(int square) {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;
        for (int f_idx = file + 1; f_idx <= 6; f_idx++) attacks |= 1UL << (rank * 8 + f_idx);
        for (int f_idx = file - 1; f_idx >= 1; f_idx--) attacks |= 1UL << (rank * 8 + f_idx);
        for (int r_idx = rank + 1; r_idx <= 6; r_idx++) attacks |= 1UL << (r_idx * 8 + file);
        for (int r_idx = rank - 1; r_idx >= 1; r_idx--) attacks |= 1UL << (r_idx * 8 + file);
        return attacks;
    }

    private static ulong bit_orthogonal_attack_blocked(int square, ulong occupancy) {
        ulong attacks = 0UL;
        int rank = square / 8;
        int file = square % 8;
        for (int f_idx = file + 1; f_idx <= 7; f_idx++) {
            attacks |= 1UL << (rank * 8 + f_idx);
            if (get_bit(occupancy, (rank * 8 + f_idx))) {
                break;
            }
        }
        for (int f_idx = file - 1; f_idx >= 0; f_idx--) { 
            attacks |= 1UL << (rank * 8 + f_idx);
            if (get_bit(occupancy, (rank * 8 + f_idx))) {
                break;
            }
        }
        for (int r_idx = rank + 1; r_idx <= 7; r_idx++) { 
            attacks |= 1UL << (r_idx * 8 + file);
            if (get_bit(occupancy, (r_idx * 8 + file))) {
                break;
            }
        }
        for (int r_idx = rank - 1; r_idx >= 0; r_idx--) { 
            attacks |= 1UL << (r_idx * 8 + file);
            if (get_bit(occupancy, (r_idx * 8 + file))) {
                break;
            }
        }
        return attacks;
    }

    private static ulong set_occupancy(int index, int num_bits, ulong attack_mask) {
        ulong occupancy = 0UL;
        for (int i = 0; i < num_bits; i++) {
            int square = pop_lsb(ref attack_mask);
            if ((index & (1 << i)) > 0) {
                occupancy |= (1UL << square);
            }
        }
        return occupancy;
    }

    private void init_pawn_attacks() {
        pawn_attack_lut = new ulong[2, 64];
        for (int i = 0; i < 64; i++) {
            pawn_attack_lut[(int)BitColor.WHITE, i] = bit_pawn_attack(i, (int)BitColor.WHITE);
            pawn_attack_lut[(int)BitColor.BLACK, i] = bit_pawn_attack(i, (int)BitColor.BLACK);
        }
    }

    private void init_knight_attacks() {
        knight_attack_lut = new ulong[64];
        for (int i = 0; i < 64; i++) {
            knight_attack_lut[i] = bit_knight_attack(i);
        }
    }

    private void init_king_attacks() {
        king_attack_lut = new ulong[64];
        for (int i = 0; i < 64; i++) {
            king_attack_lut[i] = bit_king_attack(i);
        }
    }

    private void init_diagonal_attacks() {
        bishop_mask_lut = new ulong[64];
        bishop_attack_lut = new ulong[64, 512];
        diagonal_relevant_bits = new int[64];
        for (int i = 0; i < 64; i++) {
            ulong mask = bit_diagonal_attack(i);
            bishop_mask_lut[i] = mask;
            int relevant_bits = pop_count(mask);
            int occupancy_ceil = 1 << relevant_bits;
            for (int j = 0; j < occupancy_ceil; j++) {
                ulong occupancy = set_occupancy(j, relevant_bits, mask);
                int magic_index = get_magic_index(occupancy, diagonal_magic_numbers[i], relevant_bits);
                bishop_attack_lut[i, magic_index] = bit_diagonal_attack_blocked(i, occupancy);
            }
            diagonal_relevant_bits[i] = relevant_bits;
        }
    }

    private void init_orthogonal_attacks() {
        rook_mask_lut = new ulong[64];
        rook_attack_lut = new ulong[64, 4096];
        orthogonal_relevant_bits = new int[64];
        for (int i = 0; i < 64; i++) {
            ulong mask = bit_orthogonal_attack(i);
            rook_mask_lut[i] = mask;
            int relevant_bits = pop_count(mask);
            int occupancy_ceil = 1 << relevant_bits;
            for (int j = 0; j < occupancy_ceil; j++) {
                ulong occupancy = set_occupancy(j, relevant_bits, mask);
                int magic_index = get_magic_index(occupancy, orthogonal_magic_numbers[i], relevant_bits);
                rook_attack_lut[i, magic_index] = bit_orthogonal_attack_blocked(i, occupancy);
            }
            orthogonal_relevant_bits[i] = relevant_bits;
        }
    }

    private bool is_square_attacked(BitSquare square, BitColor attacker) {
        int not_attacker = 1 - (int)attacker;
        // This is a hack for branchless execution.
        // WHITE and WhitePawn (P) are defined as 0
        // BLACK is defined as 1, so when multiplied together
        // allows the selection of opposing piece without branching
        int offset = (int)attacker * (int)(BitPiece.p);
        int pawn_idx = offset + (int)(BitPiece.P);
        ulong is_pawn_attack = pawn_attack_lut[not_attacker, (int)square] & bitboards[pawn_idx];
        // if (is_pawn_attack > 0) return true;
        int knight_idx = offset + (int)(BitPiece.N);
        ulong is_knight_attack = knight_attack_lut[(int)square] & bitboards[knight_idx];
        // if (is_knight_attack > 0) return true;
        int king_idx = offset + (int)(BitPiece.K);
        ulong is_king_attack = king_attack_lut[(int)square] & bitboards[king_idx];
        // if (is_king_attack > 0) return true;
        int bishop_idx = offset + (int)(BitPiece.B);
        ulong is_bishop_attack = get_bishop_attacks((int)square, occupancies[(int)BitColor.ALL]) & bitboards[bishop_idx];
        // if (is_bishop_attack > 0) return true;
        int rook_idx = offset + (int)(BitPiece.R);
        ulong is_rook_attack = get_rook_attacks((int)square, occupancies[(int)BitColor.ALL]) & bitboards[rook_idx];
        // if (is_rook_attack > 0) return true;
        int queen_idx = offset + (int)(BitPiece.Q);
        ulong is_queen_attack = get_queen_attacks((int)square, occupancies[(int)BitColor.ALL]) & bitboards[queen_idx];
        // if (is_queen_attack > 0) return true;
        return (is_pawn_attack   | is_knight_attack | is_king_attack
              | is_bishop_attack | is_rook_attack   | is_queen_attack) > 0;
        // return false;
    }

    private void show_attack_board(BitColor attacker) {
        StringBuilder log = new StringBuilder();
        for (int rank = 0; rank < 8; rank++) {
            log.Append(8 - rank + "  ");
            for (int file = 0; file < 8; file++) {
                log.Append((is_square_attacked((BitSquare)(rank * 8 + file), attacker) ? '1' : '0') + " ");
            }
            log.Append("\n");
        }
        log.Append("\n   ");
        for (int file = 0; file < 8; file++) {
            log.Append((char)('a' + (file)) + " ");
        }
        Debug.Log(log);
    }

    public void generate_moves(BitColor side) {
        if (side == BitColor.WHITE) {
            ulong bitboard = bitboards[(int)BitPiece.P];
            while (bitboard > 0) {
                int src = pop_lsb(ref bitboard);
                int tgt = src - 8;
                if (tgt >= 0 && !get_bit(occupancies[COL_ALL], tgt)) {
                    if (src / 8 == 1) { // promote
                        Debug.Log($"pawn promote src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                    else {
                        if (src / 8 == 6 && tgt >= 8 && !get_bit(occupancies[COL_ALL], tgt - 8)) {
                            Debug.Log($"pawn double push src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt - 8)}");
                        }
                        Debug.Log($"pawn push src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                }
                ulong attacks = pawn_attack_lut[COL_WHITE, src] & occupancies[COL_BLACK];
                while (attacks > 0) {
                    tgt = pop_lsb(ref attacks);
                    if (src / 8 == 1) {
                        Debug.Log($"pawn capture promote src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                    else {
                        Debug.Log($"pawn capture src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                }
                if (en_passant_square != BitSquare.invalid) {
                    ulong is_en_passant = pawn_attack_lut[COL_WHITE, src] & (1UL << (int)en_passant_square);
                    if (is_en_passant > 0) {
                        int en_passant_tgt = get_lsb_index(is_en_passant);
                        Debug.Log($"en passant src:{(BitSquare)(src)} tgt: {(BitSquare)(en_passant_tgt)}");
                    }
                }
            }
        }
        else {
            ulong bitboard = bitboards[(int)BitPiece.p];
            while (bitboard > 0) {
                int src = pop_lsb(ref bitboard);
                int tgt = src + 8;
                if (tgt <= 63 && !get_bit(occupancies[COL_ALL], tgt)) {
                    if (src / 8 == 6) { // promote
                        Debug.Log($"pawn promote src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                    else {
                        if (src / 8 == 1 && tgt <= 55 && !get_bit(occupancies[COL_ALL], tgt + 8)) {
                            Debug.Log($"pawn double push src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt + 8)}");
                        }
                        Debug.Log($"pawn push src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                }
                ulong attacks = pawn_attack_lut[COL_BLACK, src] & occupancies[COL_WHITE];
                while (attacks > 0) {
                    tgt = pop_lsb(ref attacks);
                    if (src / 8 == 6) {
                        Debug.Log($"pawn capture promote src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                    else {
                        Debug.Log($"pawn capture src:{(BitSquare)(src)} tgt: {(BitSquare)(tgt)}");
                    }
                }
                if (en_passant_square != BitSquare.invalid) {
                    ulong is_en_passant = pawn_attack_lut[COL_BLACK, src] & (1UL << (int)en_passant_square);
                    if (is_en_passant > 0) {
                        int en_passant_tgt = get_lsb_index(is_en_passant);
                        Debug.Log($"en passant src:{(BitSquare)(src)} tgt: {(BitSquare)(en_passant_tgt)}");
                    }
                }
            }
        }
        for (BitPiece piece = BitPiece.P; piece < BitPiece.invalid; piece++) {
            ulong bitboard = bitboards[(int)piece];
        }
    }

    public static void print_bitboard(ulong bitboard) {
        StringBuilder log = new StringBuilder();
        for (int rank = 0; rank < 8; rank++) {
            log.Append(8 - rank + "  ");
            for (int file = 0; file < 8; file++) {
                log.Append((get_bit(bitboard, (rank * 8 + file)) ? '1' : '0') + " ");
            }
            log.Append("\n");
        }
        log.Append("\n   ");
        for (int file = 0; file < 8; file++) {
            log.Append((char)('a' + (file)) + " ");
        }
        log.Append("\nDecimal: " + bitboard);
        Debug.Log(log);
    }

    public void print_board() {
        StringBuilder board = new StringBuilder();
        for (int rank = 0; rank < 8; rank++) {
            board.Append(8 - rank + " ");
            for (int file = 0; file < 8; file++) {
                int square = rank * 8 + file;
                BitPiece piece = BitPiece.invalid;
                for (piece = BitPiece.P; piece < BitPiece.invalid; piece++) {
                    if (get_bit(bitboards[(int)piece], square)) {
                        break;
                    }
                }
                board.Append($" {(piece != BitPiece.invalid ? get_piece_char(piece) : '.')}");
            }
            board.AppendLine("");
        }
        board.Append("\n   ");
        for (int file = 0; file < 8; file++) {
            board.Append((char)('a' + (file)) + " ");
        }
        board.Append("\nSide      : " + (side_to_move == BitColor.WHITE ? "WHITE" : "BLACK"));
        board.Append("\nEn Passant: " + en_passant_square.ToString());
        board.Append("\nCastling  : " + ((castling_rights & (char)BitCastling.WK) > 0 ? 'K' : '-')
                                      + ((castling_rights & (char)BitCastling.WQ) > 0 ? 'Q' : '-')
                                      + ((castling_rights & (char)BitCastling.BK) > 0 ? 'k' : '-')
                                      + ((castling_rights & (char)BitCastling.BQ) > 0 ? 'q' : '-'));
        Debug.Log(board);
    }

    public static void init_pcg(uint init_state, uint init_stream) {
        pcg_state = 0;
        pcg_increment = (init_stream << 1) | 1;
        pcg_hash();
        pcg_state += init_state;
        pcg_hash();
    }

    public static uint pcg_hash() {
        uint rand = pcg_state * 747796405u + 2891336453u;
        uint word = ((rand >> (int)((rand >> 28) + 4u)) ^ rand) * 277803737u;
        pcg_state = (word >> 22) ^ word;
        return pcg_state;
    }

    public static ulong random_u64() {
        ulong n0, n1, n2, n3;
        n0 = pcg_hash() & 0xffff;
        n1 = pcg_hash() & 0xffff;
        n2 = pcg_hash() & 0xffff;
        n3 = pcg_hash() & 0xffff;
        return n3 | (n2 << 16) | (n0 << 32) | (n1 << 48);
    }

    public static ulong random_u64_magic() {
        return random_u64() & random_u64() & random_u64();
    }

    public static ulong find_diagonal_magic_number(int square, int relevant_bits, bool is_diagonal, int max_iters) {
        ulong[] occupancies = new ulong[4096];
        ulong[] attacks = new ulong[4096];
        ulong[] magic_attack_lut = new ulong[4096];
        ulong mask = is_diagonal ? bit_diagonal_attack(square) : bit_orthogonal_attack(square);
        int occupancy_ceil = 1 << relevant_bits;
        for (int i = 0; i < occupancy_ceil; i++) {
            ulong occupancy = set_occupancy(i, relevant_bits, mask);
            occupancies[i] = occupancy;
            attacks[i] = is_diagonal ? bit_diagonal_attack_blocked(square, occupancy)
                                     : bit_orthogonal_attack_blocked(square, occupancy);
        }
        for (int iter = 0; iter < max_iters; iter++) {
            ulong magic_number = random_u64_magic();
            if (pop_count(((mask * magic_number) >> 56) & 0xff) < 6) {
                continue;
            }
            for (int i = 0; i < 4096; i++) {
                magic_attack_lut[i] = 0UL;
            }
            bool success = true;
            for (int i = 0; i < occupancy_ceil; i++) {
                int magic_idx = (int)((occupancies[i] * magic_number) >> (64 - relevant_bits));
                if (magic_attack_lut[magic_idx] == 0UL) {
                    magic_attack_lut[magic_idx] = attacks[i];
                }
                else {
                    success = false;
                    break;
                }
            }
            if (success) {
                return magic_number;
            }
        }
        Debug.Log("ERROR: While generating magic number.");
        return 0UL;
    }

    public void init_magic_numbers(bool pre_calculated, bool log) {
        if (!pre_calculated) {
            diagonal_magic_numbers = new ulong[64];
            orthogonal_magic_numbers = new ulong[64];
            for (int i = 0; i < 64; i++) {
                int relevant_diagonal_bits = pop_count(bit_diagonal_attack(i));
                int relevant_orthogonal_bits = pop_count(bit_orthogonal_attack(i));
                diagonal_magic_numbers[i] = find_diagonal_magic_number(i, relevant_diagonal_bits, true, 1000000);
                orthogonal_magic_numbers[i] = find_diagonal_magic_number(i, relevant_orthogonal_bits, false, 1000000);
                
            }
            if (log) {
                string cmd_log = "DIA:\n";
                for (int i = 0; i < 64; i++) {
                    cmd_log += diagonal_magic_numbers[i] + "UL\n";
                }
                cmd_log += "\nORTHO:\n";
                for (int i = 0; i < 64; i++) {
                    cmd_log += orthogonal_magic_numbers[i] + "UL\n";
                }
                Debug.Log(cmd_log);
            }
        }
        else { 
            diagonal_magic_numbers = new ulong[64] {
                22606027862059840UL,
                45040965652062208UL,
                2310351283111872529UL,
                1225612971393155072UL,
                1442366017614381312UL,
                577025008094871560UL,
                1297617793306329092UL,
                5765313960598307864UL,
                289954549234466885UL,
                5190682607603548224UL,
                8108767438809730048UL,
                9318092794662748160UL,
                1152925920370360386UL,
                11567778222222508608UL,
                1102230601986UL,
                4611687120220801024UL,
                2342997741322375680UL,
                1585830036103495938UL,
                4612882493241065730UL,
                281509405065216UL,
                2739314499152581396UL,
                562969424431108UL,
                2307048082698952712UL,
                1443553369376686593UL,
                2322754959464512UL,
                326003342479196678UL,
                1154065067046617473UL,
                1154337675651186720UL,
                16213526008714789392UL,
                6090152042578330112UL,
                9299671564550402UL,
                1801585124475734088UL,
                5774775842020270848UL,
                18313474397439024UL,
                2305984026150702082UL,
                9511672816647930369UL,
                1170938110730051713UL,
                72241225373188736UL,
                14412662866647753728UL,
                220818227331351044UL,
                1157583451356930818UL,
                4616348533052678144UL,
                4611967820392042496UL,
                2454572023230235136UL,
                1159685721687065600UL,
                36600611969446948UL,
                288795560125664032UL,
                565157568813572UL,
                4648804188954640UL,
                13835094373560258640UL,
                9206281735783522UL,
                9331458429003270144UL,
                4471094640640UL,
                9260536148365805904UL,
                1805956954199425024UL,
                73325348192026688UL,
                36310443828072576UL,
                576461319306347029UL,
                9223389637648654592UL,
                11533718647979673600UL,
                9223514990823213184UL,
                1161928980979777664UL,
                144185626680428609UL,
                23090302663934720UL
            };
            orthogonal_magic_numbers = new ulong[64] {
                2341871944746209408UL,
                1170971087756869632UL,
                72075323679705152UL,
                324263639937187969UL,
                10376575154959421448UL,
                1297054293458748424UL,
                288252643679209474UL,
                4683743887896871042UL,
                1234408512516259872UL,
                5190539445480210433UL,
                180566403726712832UL,
                1153765998292172824UL,
                281543780075522UL,
                4918212276806222848UL,
                585749443731522048UL,
                144396671650906368UL,
                22562528366182416UL,
                4629705915040284928UL,
                4692751362021134337UL,
                4645986517389314UL,
                2882445048828397568UL,
                5764757061183479872UL,
                72061992119076929UL,
                3459611137841579171UL,
                2310418081393967104UL,
                2305878194663723008UL,
                72339142033285120UL,
                9359223365329408UL,
                4404489486720UL,
                301745575227949184UL,
                2306002455584309776UL,
                72058152385265924UL,
                576883239705116704UL,
                306280027840839752UL,
                362856431709327360UL,
                864699926704033793UL,
                1126174919491712UL,
                3463760711971311648UL,
                44261852578320UL,
                9259682792067827972UL,
                180144260509679617UL,
                1169955413639364641UL,
                576531258503397392UL,
                11141923074597781512UL,
                10108329432436506632UL,
                3030926947300442240UL,
                1168264921096UL,
                1126571012390916UL,
                2377971521768399104UL,
                1159115054214320640UL,
                35184640557184UL,
                10524912400032333952UL,
                20266215654035712UL,
                144964011086086272UL,
                1549246511850496UL,
                9685036079769716224UL,
                140738194096385UL,
                1206965288547913797UL,
                9017095132028929UL,
                9224089330903953409UL,
                576742261641711643UL,
                563019767875586UL,
                27023899879346180UL,
                9223372587348804866UL
            };
        }
    }

    //public ulong[] legacy_converter(Piece[,] board) {
    //    ulong bitboard = 0;
    //    for (int rank = 0; rank < board.Length; rank++) {
    //        for (int file = 0; file < board.Length; file++) {
    //            if (board[rank, file] != null) {
    //                bitboard = set_bit(bitboard, rank * 8 + file);
    //            }
    //        }
    //    }
    //    return null;
    //}

    public static void test_init() {
        init_pcg(0, 0);
        BitBoardMoveGenerator move_gen = new BitBoardMoveGenerator();
        move_gen.load_fen(EVAL_POS);
        move_gen.print_board();
        move_gen.generate_moves(BitColor.WHITE);
    }
}
