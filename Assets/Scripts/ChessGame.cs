using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

public struct Coordinate {
    public int rank;
    public int file;

    public Coordinate(int rank, int file) {
        this.rank = rank;
        this.file = file;
    }
};

public static class ChessGame {
    public static int turn;
    public static bool is_check_mate;

    public static BitBoardMoveGenerator generator;

    static ChessGame() {
        turn = 0;
        generator = new BitBoardMoveGenerator();
        //load_fen("rnbqkbnr/p1p1pppp/1p6/3pP3/8/8/PPPP1PPP/RNBQKBNR w KQkq d6 0 1"); // En passant pos
        //load_fen("4RQ2/1B6/8/B2pb3/2Pk2p1/6P1/4P3/3K4 w - - 0 1");
        //load_fen("7n/3N1Np1/4k3/6Bp/2K5/5p2/Q7/4n3 w - - 0 1");
        //load_fen("3r1rk1/pp4bp/6p1/q3p2P/4n3/2N1B3/PPP1QPP1/R3K2R b - - 0 1");
        //load_fen("r3k1r1/pp2np2/4p2Q/3pP2p/5P2/3B3P/Pq4P1/R4R1K w q - 0 22");
    }

    public static void load_fen(string FEN) {
        generator.load_fen(FEN);
    }

    public static BitPiece pick_up(Coordinate square) {
        return generator.get_piece_at((7 - square.rank) * 8 + square.file);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> generate_moves(BitColor side) {
        return generator.generate_moves(side);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> generate_moves_auto() {
        return generator.generate_moves(generator.side_to_move);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static List<int> generate_moves_auto_sorted() {
        return generator.generate_moves_sorted(generator.side_to_move);
    }

    public static List<int> generate_legal_moves_auto() { 
        List<int> moves = generator.generate_moves(generator.side_to_move);
        List<int> legal_moves = new List<int>(moves.Count);
        BoardState state = new BoardState(generator);
        for (int i = 0; i < moves.Count; i++) {
            int cur_move = moves[i];
            if (generator.make_move(cur_move)) {
                legal_moves.Add(cur_move);
            }
            state.restore_state(generator);
        }
        return legal_moves;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void make_move(int move) {
        generator.make_move(move);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void restore_state(BoardState state) {
        state.restore_state(generator);
    }

}
