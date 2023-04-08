using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphaBetaTreeAI : IChessAI {
    private int depth;
    private const int NEGATIVE_INF = -999999999; // int.MinValue overflows
    private const int POSITIVE_INF = 999999999;
    private int evaluated_moves;

    public AlphaBetaTreeAI(int depth) {
        this.depth = depth;
    }

    public int play_turn() {
        evaluated_moves = 0;
        return alpha_beta_tree_search(depth);
    }

    public int get_evaluated_moves() {
        return evaluated_moves;
    }

    private int alpha_beta_tree_search(int depth) {
        List<int> all_moves = ChessGame.generator.generate_moves(ChessGame.generator.side_to_move);
        int max_score = NEGATIVE_INF - depth;
        int max_move = all_moves[0];
        evaluated_moves = all_moves.Count;
        foreach (int m in all_moves) {
            BoardState state = new BoardState(ChessGame.generator);
            bool valid_move = ChessGame.generator.make_move(m);
            if (!valid_move) {
                continue;
            }
            int cur_score = alpha_beta_tree_search_aux(depth - 1, NEGATIVE_INF, POSITIVE_INF, false);
            state.restore_state(ChessGame.generator);
            if (cur_score > max_score) {
                max_score = cur_score;
                max_move = m;
            }
        }
        return max_move;
    }

    private int alpha_beta_tree_search_aux(int depth, int alpha, int beta, bool maximizing_player) {
        if (depth == 0) {
            return evaluate_board();
        }
        List<int> all_moves = ChessGame.generator.generate_moves(ChessGame.generator.side_to_move);
        evaluated_moves += all_moves.Count;
        BoardState state = new BoardState(ChessGame.generator);
        int value;
        if (maximizing_player) {
            value = NEGATIVE_INF - depth;
            foreach (int m in all_moves) {
                bool valid_move = ChessGame.generator.make_move(m);
                if (!valid_move) {
                    continue;
                }
                value = Mathf.Max(value, alpha_beta_tree_search_aux(depth - 1, alpha, beta, false));
                state.restore_state(ChessGame.generator);
                alpha = Mathf.Max(alpha, value);
                if (value >= beta) {
                    break;
                }
            }
        }
        else {
            value = POSITIVE_INF + depth;
            foreach (int m in all_moves) {
                bool valid_move = ChessGame.generator.make_move(m);
                if (!valid_move) {
                    continue;
                }
                value = Mathf.Min(value, alpha_beta_tree_search_aux(depth - 1, alpha, beta, true));
                state.restore_state(ChessGame.generator);
                beta = Mathf.Min(beta, value);
                if (value <= alpha) {
                    break;
                }
            }
        }
        return value;
    }

    // TODO: Need IScoringFunction for generalization as well
    public int evaluate_board() {
        int score = 0;
        int white_mul = ChessGame.generator.side_to_move == BitColor.WHITE ? 1 : -1;
        int black_mul = -white_mul;
        List<int> all_moves = ChessGame.generator.generate_moves(ChessGame.generator.side_to_move);
        if (all_moves.Count == 0) {
            return black_mul * int.MaxValue;
        }
        for (int i = (int)BitPiece.P; i <= (int)BitPiece.K; i++) {
            score += white_mul * BitBoardMoveGenerator.pop_count(ChessGame.generator.bitboards[i]) * piece_score((BitPiece)(i % (int)BitPiece.p));
        }
        for (int i = (int)BitPiece.p; i <= (int)BitPiece.k; i++) {
            score += black_mul * BitBoardMoveGenerator.pop_count(ChessGame.generator.bitboards[i]) * piece_score((BitPiece)(i % (int)BitPiece.p));
        }
        return score;
    }

    // TODO: Convert to map? Generalize? IDK
    public int piece_score(BitPiece p) {
        switch (p) {
            case BitPiece.P: return 100;
            case BitPiece.K: return 99999; // Needs to be arbitrarily large
            case BitPiece.N: return 300;
            case BitPiece.B: return 350;
            case BitPiece.Q: return 900;
            case BitPiece.R: return 500;
        }
        return 0;
    }
}
