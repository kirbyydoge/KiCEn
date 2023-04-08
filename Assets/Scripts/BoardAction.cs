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
    private BitPiece held_piece;
    private Coordinate begin;
    private Coordinate end;
    private BoardRenderer board_renderer;
    private SpriteRenderer held_renderer;
    private List<int> all_moves;
    private List<int> available_moves;
    private List<BoardState> earlier_states;
    private bool moves_valid;
    private bool notify_flag;
    private float start_timer;

    void Start() {
        earlier_states = new List<BoardState>();
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
                p2_ai = new AlphaBossAI(p2_search_depth);
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
            all_moves = ChessGame.generate_legal_moves_auto();
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
                    earlier_states.Add(new BoardState(ChessGame.generator));
                    state = p1_ai_type == AIType.Human ? ActionState.EMPTY : ActionState.OPPONENT;
                }
                break;
            case ActionState.EMPTY:
                if (Input.GetKeyDown(KeyCode.R)) {
                    if (earlier_states.Count > 1) {
                        ChessGame.restore_state(earlier_states[earlier_states.Count - 2]);
                        earlier_states.RemoveAt(earlier_states.Count - 1);
                        board_renderer.render_pieces();
                        moves_valid = false;
                    }
                }
                if (Input.GetMouseButton(0)) {
                    begin = screen_to_board_coordinate(world_pos);
                    held_piece = ChessGame.pick_up(begin);
                    if (held_piece != BitPiece.invalid) {
                        int source = (7 - begin.rank) * 8 + begin.file;
                        state = ActionState.MOVING;
                        held_renderer.transform.position = world_pos;
                        held_renderer.sprite = board_renderer.get_sprite(begin);
                        held_renderer.enabled = true;
                        board_renderer.disable_cell(begin);
                        available_moves = all_moves.FindAll(move =>
                            BitBoardMoveGenerator.get_source(move) == source);
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
                        earlier_states.Add(new BoardState(ChessGame.generator));
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
                int ai_move = cur_ai.play_turn();
                float stop = Time.realtimeSinceStartup;
                ChessGame.make_move(ai_move);
                Debug.Log("Evaluated " + cur_ai.get_evaluated_moves() + " positions in " + (stop - start) + " s.");
                BitBoardMoveGenerator.BitMove parsed = new BitBoardMoveGenerator.BitMove(ai_move);
                Debug.Log($"{(BitPiece)parsed.piece}: {(BitSquare)parsed.source} -> {(BitSquare)parsed.target}");
                earlier_states.Add(new BoardState(ChessGame.generator));
                moves_valid = false;
                board_renderer.render_pieces();
                p1_turn = !p1_turn;
                break;
            case ActionState.GAME_OVER:
                if (Input.GetKeyDown(KeyCode.R)) {
                    notify_flag = true;
                    if (earlier_states.Count > 1) {
                        ChessGame.restore_state(earlier_states[earlier_states.Count - 2]);
                        earlier_states.RemoveAt(earlier_states.Count - 1);
                        board_renderer.render_pieces();
                        moves_valid = false;
                    }
                }
                break;
        }
    }

    int select_move(List<int> available_moves, Coordinate end) {
        int selected_move = -1;
        end.rank = 7 - end.rank;
        if (available_moves != null) { 
            for (int i = 0; i < available_moves.Count; i++) {
                int move = available_moves[i];
                int target = BitBoardMoveGenerator.get_target(move);
                if (target / 8 == end.rank && target % 8 == end.file) {
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
