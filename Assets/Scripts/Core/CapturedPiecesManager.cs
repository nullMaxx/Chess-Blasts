using UnityEngine;
using UnityEngine.UI;

public class CapturedPiecesManager : MonoBehaviour
{
    public GameObject capturedPiecePrefab;
    public Transform capturedPiecesPanel;

    public void AddCapturedPiece(Sprite pieceSprite)
    {
        GameObject capturedPiece = Instantiate(capturedPiecePrefab, capturedPiecesPanel);
        
        Image pieceImage = capturedPiece.GetComponent<Image>();
        if (pieceImage != null)
        {
            pieceImage.sprite = pieceSprite;
        }
    }
}
