using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardAction : MonoBehaviour {

    public enum AIType {
        Human, Random, NaiveTree, AlphaBetaTree, AlphaBossAI
    };

    enum ActionState { 
        START, EMPTY, MOVING, OPPONENT, GAME_OVER
    };

    public Camera main_camera;
    public AIType p1_ai_type;
    public int p1_search_depth = 5;
    public AIType p2_ai_type;
    public int p2_search_depth = 5;

    private IChessAI p1_ai;
    private IChessAI p2_ai;
    private bool p1_turn;
    private ActionState state;
    private Piece held_piece;
    private Coordinate begin;
    private Coordinate end;
    private BoardRenderer board_renderer;
    private SpriteRenderer held_renderer;
    private List<Move> available_moves;
    private List<Move> played_moves;
    private bool moves_valid;
    private List<List<Move>> all_moves;
    private bool notify_flag;
    private float start_timer;

    void Start() {
        played_moves = new List<Move>();
        start_timer = 0.0f;
        state = ActionState.START;
        begin.rank = -1;
        begin.file = -1;
        board_renderer = gameObject.GetComponent<BoardRenderer>();
        held_renderer = gameObject.AddComponent<SpriteRenderer>();
        held_renderer.transform.localScale = Vector3.one * 2.5f;
        held_renderer.enabled = false;
        available_moves = null;
        moves_valid = false;
        switch (p1_ai_type) {
            case AIType.Random:
                p1_ai = new RandomAI();
                break;
            case AIType.NaiveTree:
                p1_ai = new NaiveTreeAI(p1_search_depth);
                break;
            case AIType.AlphaBetaTree:
                p1_ai = new AlphaBetaTreeAI(p1_search_depth);
                break;
            case AIType.AlphaBossAI:
                p1_ai = new AlphaBossAI(p1_search_depth);
                break;
        }
        switch (p2_ai_type) {
            case AIType.Random:
                p2_ai = new RandomAI();
                break;
            case AIType.NaiveTree:
                p2_ai = new NaiveTreeAI(p2_search_depth);
                break;
            case AIType.AlphaBetaTree:
                p2_ai = new AlphaBetaTreeAI(p2_search_depth);
                break;
            case AIType.AlphaBossAI:
                p2_ai = new AlphaBossAI(p1_search_depth);
                break;
        }
        notify_flag = true;
        p1_turn = true;
    }

    void Update() {
        Vector3 mouse_pos = Input.mousePosition;
        Vector3 world_pos = main_camera.ScreenToWorldPoint(mouse_pos);
        AIType next_ai = p1_turn ? p2_ai_type : p1_ai_type;
        IChessAI cur_ai = p1_turn ? p1_ai : p2_ai;
        world_pos.z = 0;
        if (!moves_valid) {
            all_moves = ChessGame.generate_all_moves_auto();
            moves_valid = true;
        }
        if (ChessGame.is_check_mate) {
            state = ActionState.GAME_OVER;
            if (notify_flag) {
                notify_flag = false;
                Debug.Log("Check mate!");
            }
        }
        switch (state) {
            case ActionState.START:
                start_timer += Time.deltaTime;
                if (start_timer > 0.1) {
                    state = p1_ai_type == AIType.Human ? ActionState.EMPTY : ActionState.OPPONENT;
                }
                break;
            case ActionState.EMPTY:
                if (Input.GetKeyDown(KeyCode.R)) {
                    if (played_moves.Count > 0) {
                        ChessGame.unmake_move(played_moves[played_moves.Count - 1]);
                        played_moves.RemoveAt(played_moves.Count - 1);
                        board_renderer.render_pieces();
                        moves_valid = false;
                    }
                }
                if (Input.GetMouseButton(0)) {
                    begin = screen_to_board_coordinate(world_pos);
                    held_piece = ChessGame.pick_up(begin);
                    if (held_piece != null) {
                        state = ActionState.MOVING;
                        held_renderer.transform.position = world_pos;
                        held_renderer.sprite = board_renderer.get_sprite(begin);
                        held_renderer.enabled = true;
                        board_renderer.disable_cell(begin);
                        available_moves = all_moves[held_piece.lookup_index];
                        board_renderer.render_pieces();
                        board_renderer.render_moves(available_moves);
                    }
                }
                break;
            case ActionState.MOVING:
                held_renderer.transform.position = world_pos;
                if (!Input.GetMouseButton(0)) {
                    state = ActionState.EMPTY;
                    end = screen_to_board_coordinate(world_pos);
                    int selected_move = select_move(available_moves, end);
                    if (selected_move >= 0) {
                        state = next_ai == AIType.Human ? ActionState.EMPTY : ActionState.OPPONENT;
                        p1_turn = !p1_turn;
                        ChessGame.make_move(available_moves[selected_move]);
                        played_moves.Add(available_moves[selected_move]);
                        moves_valid = false;
                    }
                    held_renderer.enabled = false;
                    board_renderer.enable_cell(begin);
                    board_renderer.render_pieces();
                }
                break;
            case ActionState.OPPONENT:
                state = next_ai == AIType.Human ? ActionState.EMPTY : ActionState.OPPONENT;
                float start = Time.realtimeSinceStartup;
                Move ai_move = cur_ai.play_turn();
                float stop = Time.realtimeSinceStartup;
                ChessGame.make_move(ai_move);
                Debug.Log("Evaluated " + cur_ai.get_evaluated_moves() + " positions in " + (stop - start) + " s.");
                played_moves.Add(ai_move);
                moves_valid = false;
                board_renderer.render_pieces();
                p1_turn = !p1_turn;
                break;
            case ActionState.GAME_OVER:
                if (Input.GetKeyDown(KeyCode.R)) {
                    notify_flag = true;
                    if (played_moves.Count > 0) {
                        state = ActionState.EMPTY;
                        ChessGame.unmake_move(played_moves[played_moves.Count - 1]);
                        played_moves.RemoveAt(played_moves.Count - 1);
                        board_renderer.render_pieces();
                        moves_valid = false;
                    }
                }
                break;
        }
    }

    int select_move(List<Move> available_moves, Coordinate end) {
        int selected_move = -1;
        if (available_moves != null) { 
            for (int i = 0; i < available_moves.Count; i++) {
                Move move = available_moves[i];
                if (move.end.rank == end.rank && move.end.file == end.file) {
                    selected_move = i;
                    break;
                }
            }
        }
        return selected_move;
    }

    Coordinate screen_to_board_coordinate(Vector3 world_pos) {
        float offset = board_renderer.tile_scale / 2;
        Coordinate cell;
        cell.rank = (int)((world_pos.y + offset) / (board_renderer.tile_scale));
        cell.file = (int)((world_pos.x + offset) / (board_renderer.tile_scale));
        return cell;
    }
}
