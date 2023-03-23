using System.Collections;
using System.Collections.Generic;
using System;
//using UnityEngine;

public class BitBoardMoveGenerator {
    private enum BitPiece { 
        PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
    };

    private enum BitColor { 
        WHITE, BLACK
    };

    private enum BitSquare {
        a8, b8, c8, d8, e8, f8, g8, h8,
        a7, b7, c7, d7, e7, f7, g7, h7,
        a6, b6, c6, d6, e6, f6, g6, h6,
        a5, b5, c5, d5, e5, f5, g5, h5,
        a4, b4, c4, d4, e4, f4, g4, h4,
        a3, b3, c3, d3, e3, f3, g3, h3,
        a2, b2, c2, d2, e2, f2, g2, h2,
        a1, b1, c1, d1, e1, f1, g1, h1
    };

    private const int from_offset = 0;
    private const int from_mask = 0x3f;
    private const int target_offset = 6;
    private const int target_mask = 0xfc0;

    // Used for pawns
    private const ulong not_a_file = 18374403900871474942UL;
    private const ulong not_h_file = 9187201950435737471UL;

    // Used for knights
    private const ulong not_hg_file = 4557430888798830399UL;
    private const ulong not_ab_file = 18229723555195321596UL;

    public ulong[,] pawn_attack_lut;
    public ulong[] knight_attack_lut;
    public ulong[] king_attack_lut;

    private static ulong set_bit(ulong number, int idx) {
        return number |= 1UL << idx;
    }
    private static ulong get_bit(ulong number, int idx) {
        return number &= 1UL << idx;
    }

    private static ulong clear_bit(ulong number, int idx) {
        return number &= ~(1UL << idx);
    }

    private static ulong bit_pawn_attack(int square, int side) {
        ulong bitboard = set_bit(0UL, square);
        ulong attacks = 0UL;
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
        ulong bitboard = set_bit(0UL, square);
        ulong attacks = 0UL;
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
        ulong bitboard = set_bit(0UL, square);
        ulong attacks = 0UL;
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

    public static void print_bitboard(ulong bitboard) {
        for (int rank = 0; rank < 8; rank++) {
            Console.Write((8 - rank) + "  ");
            for (int file = 0; file < 8; file++) {
                //Debug.Log(get_bit(bitboard, (rank * 8 + file)) > 0 ? "1 " : "0 ");
                Console.Write(get_bit(bitboard, (rank * 8 + file)) > 0 ? "1 " : "0 ");
            }
            //Debug.Log("");
            Console.WriteLine("");
        }
        Console.Write("\n   ");
        for (int file = 0; file < 8; file++) {
            //Debug.Log(get_bit(bitboard, (rank * 8 + file)) > 0 ? "1 " : "0 ");
            Console.Write((char)('a' + (file)) + " ");
        }
        Console.WriteLine("\nDecimal: " + bitboard);
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

    public static void Main(string[] args) {
        //for (int rank = 0; rank < 8; rank++) {
        //    for (int file = 0; file < 8; file++) {
        //        Console.Write((char)('a' + (file)) + "" + (8 - rank) + ", ");
        //    }
        //    Console.WriteLine("");
        //}
        BitBoardMoveGenerator move_gen = new BitBoardMoveGenerator();
        move_gen.init_pawn_attacks();
        move_gen.init_knight_attacks();
        move_gen.init_king_attacks();
        for (int i = 0; i < 64; i++) {
            //print_bitboard(move_gen.pawn_attack_lut[(int)BitColor.WHITE, i]);
            //print_bitboard(move_gen.pawn_attack_lut[(int)BitColor.BLACK, i]);
            //print_bitboard(move_gen.knight_attack_lut[i]);
            print_bitboard(move_gen.king_attack_lut[i]);
        }
        //print_bitboard(bit_knight_attack((int)BitSquare.g4));
    }
}
