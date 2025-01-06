using System.Collections;
using UnityEngine;

namespace Chess.Game {
	public class BoardUI : MonoBehaviour {
		public PieceTheme pieceTheme;
		public BoardTheme boardTheme;
		public bool showLegalMoves;

		public bool whiteIsBottom = true;

		MeshRenderer[, ] squareRenderers;
		SpriteRenderer[, ] squarePieceRenderers;
		Move lastMadeMove;
		MoveGenerator moveGenerator;

		const float pieceDepth = -0.1f;
		const float pieceDragDepth = -0.2f;

		void Awake () {
			moveGenerator = new MoveGenerator ();
			CreateBoardUI ();

		}

		public void HighlightLegalMoves(Board board, Coord fromSquare) {
    if (showLegalMoves) {
        // Define the base colors for light and dark squares
        Color gradientStartLight = new Color(220f/255f, 92f/255f, 144f/255f); // Light square gradient start
        Color gradientEndLight = new Color(169f/255f, 91f/255f, 207f/255f);   // Light square gradient end
        Color gradientStartDark = new Color(192f/255f, 84f/255f, 140f/255f);  // Dark square gradient start
        Color gradientEndDark = new Color(152f/255f, 85f/255f, 187f/255f);    // Dark square gradient end

        // Define the darker color to apply on legal moves
        Color darkenedLightStart = gradientStartLight * 0.7f; // Darker shade of light square
        Color darkenedLightEnd = gradientEndLight * 0.7f;     // Darker shade of light square
        Color darkenedDarkStart = gradientStartDark * 0.7f;   // Darker shade of dark square
        Color darkenedDarkEnd = gradientEndDark * 0.7f;       // Darker shade of dark square

        var moves = moveGenerator.GenerateMoves(board);

        for (int i = 0; i < moves.Count; i++) {
            Move move = moves[i];
            if (move.StartSquare == BoardRepresentation.IndexFromCoord(fromSquare)) {
                Coord coord = BoardRepresentation.CoordFromIndex(move.TargetSquare);

                // Apply darker color for legal move on light squares
                if (coord.IsLightSquare()) {
                    float t = (coord.fileIndex + coord.rankIndex) / 14f; // Normalized gradient factor
                    Color interpolatedColor = Color.Lerp(darkenedLightStart, darkenedLightEnd, t);
                    SetSquareColour(coord, interpolatedColor, interpolatedColor);
                }
                // Apply darker color for legal move on dark squares
                else {
                    float t = (coord.fileIndex + coord.rankIndex) / 14f; // Normalized gradient factor
                    Color interpolatedColor = Color.Lerp(darkenedDarkStart, darkenedDarkEnd, t);
                    SetSquareColour(coord, interpolatedColor, interpolatedColor);
                }
            }
        }
    }
}



		public void DragPiece (Coord pieceCoord, Vector2 mousePos) {
			squarePieceRenderers[pieceCoord.fileIndex, pieceCoord.rankIndex].transform.position = new Vector3 (mousePos.x, mousePos.y, pieceDragDepth);
		}

		public void ResetPiecePosition (Coord pieceCoord) {
			Vector3 pos = PositionFromCoord (pieceCoord.fileIndex, pieceCoord.rankIndex, pieceDepth);
			squarePieceRenderers[pieceCoord.fileIndex, pieceCoord.rankIndex].transform.position = pos;
		}

		public void SelectSquare (Coord coord) {
			SetSquareColour (coord, boardTheme.lightSquares.selected, boardTheme.darkSquares.selected);
		}

		public void DeselectSquare (Coord coord) {
			//BoardTheme.SquareColours colours = (coord.IsLightSquare ()) ? boardTheme.lightSquares : boardTheme.darkSquares;
			//squareMaterials[coord.file, coord.rank].color = colours.normal;
			ResetSquareColours ();
		}

		public bool TryGetSquareUnderMouse (Vector2 mouseWorld, out Coord selectedCoord) {
			int file = (int) (mouseWorld.x + 4);
			int rank = (int) (mouseWorld.y + 4);
			if (!whiteIsBottom) {
				file = 7 - file;
				rank = 7 - rank;
			}
			selectedCoord = new Coord (file, rank);
			return file >= 0 && file < 8 && rank >= 0 && rank < 8;
		}

		public void UpdatePosition (Board board) {
			for (int rank = 0; rank < 8; rank++) {
				for (int file = 0; file < 8; file++) {
					Coord coord = new Coord (file, rank);
					int piece = board.Square[BoardRepresentation.IndexFromCoord (coord.fileIndex, coord.rankIndex)];
					squarePieceRenderers[file, rank].sprite = pieceTheme.GetPieceSprite (piece);
					squarePieceRenderers[file, rank].transform.position = PositionFromCoord (file, rank, pieceDepth);
				}
			}

		}

		public void OnMoveMade (Board board, Move move, bool animate = false) {
			lastMadeMove = move;
			if (animate) {
				StartCoroutine (AnimateMove (move, board));
			} else {
				UpdatePosition (board);
				ResetSquareColours ();
			}
		}

		IEnumerator AnimateMove (Move move, Board board) {
			float t = 0;
			const float moveAnimDuration = 0.15f;
			Coord startCoord = BoardRepresentation.CoordFromIndex (move.StartSquare);
			Coord targetCoord = BoardRepresentation.CoordFromIndex (move.TargetSquare);
			Transform pieceT = squarePieceRenderers[startCoord.fileIndex, startCoord.rankIndex].transform;
			Vector3 startPos = PositionFromCoord (startCoord);
			Vector3 targetPos = PositionFromCoord (targetCoord);
			SetSquareColour (BoardRepresentation.CoordFromIndex (move.StartSquare), boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);

			while (t <= 1) {
				yield return null;
				t += Time.deltaTime * 1 / moveAnimDuration;
				pieceT.position = Vector3.Lerp (startPos, targetPos, t);
			}
			UpdatePosition (board);
			ResetSquareColours ();
			pieceT.position = startPos;
		}

		void HighlightMove (Move move) {
			SetSquareColour (BoardRepresentation.CoordFromIndex (move.StartSquare), boardTheme.lightSquares.moveFromHighlight, boardTheme.darkSquares.moveFromHighlight);
			SetSquareColour (BoardRepresentation.CoordFromIndex (move.TargetSquare), boardTheme.lightSquares.moveToHighlight, boardTheme.darkSquares.moveToHighlight);
		}

		void CreateBoardUI () {

			Shader squareShader = Shader.Find ("Unlit/Color");
			squareRenderers = new MeshRenderer[8, 8];
			squarePieceRenderers = new SpriteRenderer[8, 8];

			for (int rank = 0; rank < 8; rank++) {
				for (int file = 0; file < 8; file++) {
					// Create square
					Transform square = GameObject.CreatePrimitive (PrimitiveType.Quad).transform;
					square.parent = transform;
					square.name = BoardRepresentation.SquareNameFromCoordinate (file, rank);
					square.position = PositionFromCoord (file, rank, 0);
					Material squareMaterial = new Material (squareShader);

					squareRenderers[file, rank] = square.gameObject.GetComponent<MeshRenderer> ();
					squareRenderers[file, rank].material = squareMaterial;

					// Create piece sprite renderer for current square
					SpriteRenderer pieceRenderer = new GameObject ("Piece").AddComponent<SpriteRenderer> ();
					pieceRenderer.transform.parent = square;
					pieceRenderer.transform.position = PositionFromCoord (file, rank, pieceDepth);
					pieceRenderer.transform.localScale = Vector3.one * 100 / (2000 / 6f);
					squarePieceRenderers[file, rank] = pieceRenderer;
				}
			}

			ResetSquareColours ();
		}

		void ResetSquarePositions () {
			for (int rank = 0; rank < 8; rank++) {
				for (int file = 0; file < 8; file++) {
					if (file == 0 && rank == 0) {
						//Debug.Log (squarePieceRenderers[file, rank].gameObject.name + "  " + PositionFromCoord (file, rank, pieceDepth));
					}
					//squarePieceRenderers[file, rank].transform.position = PositionFromCoord (file, rank, pieceDepth);
					squareRenderers[file, rank].transform.position = PositionFromCoord (file, rank, 0);
					squarePieceRenderers[file, rank].transform.position = PositionFromCoord (file, rank, pieceDepth);
				}
			}

			if (!lastMadeMove.IsInvalid) {
				HighlightMove (lastMadeMove);
			}
		}

		public void SetPerspective (bool whitePOV) {
			whiteIsBottom = whitePOV;
			ResetSquarePositions ();

		}

		public void ResetSquareColours (bool highlight = true) {
    Color gradientStartLight = new Color(220f/255f,92f/255f,144f/255f); // Light square gradient start
    Color gradientEndLight = new Color(169f/255f,91f/255f,207f/255f);   // Light square gradient end
    Color gradientStartDark = new Color(192f/255f,84f/255f,140f/255f);  // Dark square gradient start
    Color gradientEndDark = new Color(152f/255f,85f/255f,187f/255f);    // Dark square gradient end

    for (int rank = 0; rank < 8; rank++) {
        for (int file = 0; file < 8; file++) {
            float t = (rank + file) / 14f; // Normalized gradient factor (range 0 to 1)
            if ((rank + file) % 2 == 0) {
                // Light square
                Color interpolatedColor = Color.Lerp(gradientStartLight, gradientEndLight, t);
                SetSquareColour(new Coord(file, rank), interpolatedColor, interpolatedColor);
            } else {
                // Dark square
                Color interpolatedColor = Color.Lerp(gradientStartDark, gradientEndDark, t);
                SetSquareColour(new Coord(file, rank), interpolatedColor, interpolatedColor);
            }
        }
    }

    if (highlight && !lastMadeMove.IsInvalid) {
        HighlightMove(lastMadeMove);
    }
}


		void SetSquareColour (Coord square, Color lightCol, Color darkCol) {
			squareRenderers[square.fileIndex, square.rankIndex].material.color = (square.IsLightSquare ()) ? lightCol : darkCol;
		}

		public Vector3 PositionFromCoord (int file, int rank, float depth = 0) {
			if (whiteIsBottom) {
				return new Vector3 (-3.5f + file, -3.5f + rank, depth);
			}
			return new Vector3 (-3.5f + 7 - file, 7 - rank - 3.5f, depth);

		}

		public Vector3 PositionFromCoord (Coord coord, float depth = 0) {
			return PositionFromCoord (coord.fileIndex, coord.rankIndex, depth);
		}


	}
}