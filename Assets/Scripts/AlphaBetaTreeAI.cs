using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphaBetaTreeAI : IChessAI {
    private int depth;
    private const int NEGATIVE_INF = -999999999; // int.MinValue overflows
    private const int POSITIVE_INF = 999999999;
    private static int evaluated_moves;

    public AlphaBetaTreeAI(int depth) {
        this.depth = depth;
    }

    public int play_turn() {
        evaluated_moves = 0;
        return alpha_beta_tree_search(depth, ChessGame.generator.side_to_move == BitColor.WHITE);
    }

    public int get_evaluated_moves() {
        return evaluated_moves;
    }

    public void notify_move(int move) {
        
    }

    public void retake() {

    }

    private static int alpha_beta_tree_search(int depth, bool maximizing_player) {
        List<int> all_moves = ChessGame.generate_moves_auto_sorted();
        evaluated_moves = all_moves.Count;
        BoardState state = new BoardState(ChessGame.generator);
        int best_eval;
        int best_move = all_moves[0];
        if (maximizing_player) {
            best_eval = NEGATIVE_INF;
            foreach (int m in all_moves) {
                bool valid_move = ChessGame.generator.make_move(m);
                if (!valid_move) {
                    continue;
                }
                int cur_score = alpha_beta_tree_search_aux(depth - 1, NEGATIVE_INF, POSITIVE_INF, false);
                state.restore_state(ChessGame.generator);
                if (cur_score > best_eval) {
                    best_eval = cur_score;
                    best_move = m;
                }
            }
        }
        else {
            best_eval = POSITIVE_INF;
            foreach (int m in all_moves) {
                bool valid_move = ChessGame.generator.make_move(m);
                if (!valid_move) {
                    continue;
                }
                int cur_score = alpha_beta_tree_search_aux(depth - 1, NEGATIVE_INF, POSITIVE_INF, true);
                state.restore_state(ChessGame.generator);
                if (cur_score < best_eval) {
                    best_eval = cur_score;
                    best_move = m;
                }
            }
        }
        return best_move;
    }

    private static int alpha_beta_tree_search_aux(int depth, int alpha, int beta, bool maximizing_player) {
        if (depth == 0) {
            return evaluate_board();
        }
        List<int> all_moves = ChessGame.generate_moves_auto_sorted();
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

    public static int evaluate_board() {
        int score = 0;
        int white_mul = ChessGame.generator.side_to_move == BitColor.WHITE ? 1 : -1;
        BitFinish status = ChessGame.generator.is_check_or_stale_mate();
        if (status == BitFinish.CHECKMATE) {
            return -white_mul * POSITIVE_INF;
        }
        else if (status == BitFinish.STALEMATE) {
            return 0;
        }
        for (int i = (int)BitPiece.P; i < (int)BitPiece.K; i++) {
            ulong bitboard = ChessGame.generator.bitboards[i];
            while (bitboard > 0) {
                int square = BitBoardMoveGenerator.pop_lsb(ref bitboard);
                score += PositionalScore.piece_score[i];
                if (i != (int)BitPiece.K) {
                    score += PositionalScore.positional_score[i, square];
                }
            }
        }
        for (int i = (int)BitPiece.p; i < (int)BitPiece.k; i++) {
            ulong bitboard = ChessGame.generator.bitboards[i];
            int piece_idx = i - (int)BitPiece.p;
            while (bitboard > 0) {
                int square = BitBoardMoveGenerator.pop_lsb(ref bitboard);
                square = square % 8 + (7 - square / 8) * 8; // mirror horizontally
                score -= PositionalScore.piece_score[piece_idx];
                score -= PositionalScore.positional_score[piece_idx, square];
            }
        }
        return score;
    }
}
