using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphaBossAI : IChessAI {
    private int depth;
    private const int NEGATIVE_INF = -999999999; // int.MinValue overflows
    private const int POSITIVE_INF = 999999999;
    private int evaluated_moves;
    private Logger logger;

    public AlphaBossAI(int depth) {
        this.depth = depth;
        logger = new Logger(@"C:\Users\aqwog\Desktop\AI.log");
    }

    public Move play_turn() {
        evaluated_moves = 0;
        return alpha_beta_tree_search(depth);
    }

    public int get_evaluated_moves() {
        return evaluated_moves;
    }

    private Move alpha_beta_tree_search(int depth) {
        List<Move> all_moves = ChessGame.generate_flat_moves_auto();
        int max_score = NEGATIVE_INF - depth;
        Move max_move = all_moves[0];
        evaluated_moves = all_moves.Count;
        foreach (Move m in all_moves) {
            ChessGame.make_move(m);
            int cur_score = alpha_beta_tree_search_aux(depth - 1, NEGATIVE_INF, POSITIVE_INF, false);
            ChessGame.unmake_move(m);
            if (cur_score > max_score) {
                max_score = cur_score;
                max_move = m;
            }
        }
        return max_move;
    }

    private int alpha_beta_tree_search_aux(int depth, int alpha, int beta, bool maximizing_player) {
        if (depth == 0) {
            int start_moves = evaluated_moves;
            float start = Time.realtimeSinceStartup;
            int eval = alpha_beta_tree_search_takes(alpha, beta, maximizing_player);
            int stop_moves = evaluated_moves;
            float stop = Time.realtimeSinceStartup;
            logger.WriteLine("Evaluated " + (stop_moves - start_moves) + " takes in " + (stop - start) + " s.");
            return eval;
        }
        List<Move> all_moves = ChessGame.generate_flat_moves_auto();
        evaluated_moves += all_moves.Count;
        int value;
        if (maximizing_player) {
            value = NEGATIVE_INF - depth;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Max(value, alpha_beta_tree_search_aux(depth - 1, alpha, beta, false));
                ChessGame.unmake_move(m);
                alpha = Mathf.Max(alpha, value);
                if (value >= beta) {
                    break;
                }
            }
        }
        else {
            value = POSITIVE_INF + depth;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Min(value, alpha_beta_tree_search_aux(depth - 1, alpha, beta, true));
                ChessGame.unmake_move(m);
                beta = Mathf.Min(beta, value);
                if (value <= alpha) {
                    break;
                }
            }
        }
        return value;
    }

    private int alpha_beta_tree_search_takes(int alpha, int beta, bool maximizing_player) {
        List<Move> all_moves = ChessGame.generate_flat_moves_auto().FindAll(m => m.target != null && m.castle == 0);
        if (all_moves.Count == 0) {
            return evaluate_board();
        }
        evaluated_moves += all_moves.Count;
        logger.WriteLine((maximizing_player ? "Max" : "Min") + " Takes Evaluated: " + evaluated_moves);
        foreach (Move m in all_moves) {
            logger.WriteLine($"{m.held.type} ({m.begin.rank}, {m.begin.file}) takes {m.target.type} ({m.end.rank}, {m.end.file}).");
        }
        int value;
        if (maximizing_player) {
            value = NEGATIVE_INF;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Max(value, alpha_beta_tree_search_takes(alpha, beta, false));
                ChessGame.unmake_move(m);
                alpha = Mathf.Max(alpha, value);
                if (value >= beta) {
                    break;
                }
            }
        }
        else {
            value = POSITIVE_INF;
            foreach (Move m in all_moves) {
                ChessGame.make_move(m);
                value = Mathf.Min(value, alpha_beta_tree_search_takes(alpha, beta, true));
                ChessGame.unmake_move(m);
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
        int white_mul = ChessGame.player_to_move == PieceColor.WHITE ? 1 : -1;
        int black_mul = -white_mul;
        if (ChessGame.is_check_mate) {
            return black_mul * int.MaxValue;
        }
        foreach (Piece p in ChessGame.white_pieces) {
            if (p.active) {
                score += white_mul * piece_score(p);
            }
        }
        foreach (Piece p in ChessGame.black_pieces) {
            if (p.active) {
                score += black_mul * piece_score(p);
            }
        }
        return score;
    }

    // TODO: Convert to map? Generalize? IDK
    public int piece_score(Piece p) {
        switch (p.type) {
            case PieceType.PAWN: return 100;
            case PieceType.KING: return 99999; // Needs to be arbitrarily large
            case PieceType.KNIGHT: return 300;
            case PieceType.BISHOP: return 350;
            case PieceType.QUEEN: return 900;
            case PieceType.ROOK: return 500;
        }
        return 0;
    }
}
