using System;
using Unity.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Block : MonoBehaviour, IPointerDownHandler
{
    public event Action<Vector2Int> DidGetClicked;
    
    [SerializeField] private Rigidbody2D _Rigidbody;
    [SerializeField] private BlockColor _Color;
    [Space]
    [SerializeField] private Image _DefaultIcon;
    [SerializeField] private Image _IconA;
    [SerializeField] private Image _IconB;
    [SerializeField] private Image _IconC;
    
    [SerializeField] private BlockMatchGroupType _matchType;
    
    public Rigidbody2D Rigidbody => _Rigidbody;
    public BlockColor Color => _Color;
    public Vector2Int GridPos { get; set; }

    
    public void OnPointerDown(PointerEventData eventData)
    {
        DidGetClicked?.Invoke(GridPos);
        //Debug.Log("yes here  " + GridPos);
    }

    public void SetMatchGroupType(BlockMatchGroupType matchType)
    {
        if (_matchType == matchType) return;

        _matchType = matchType;

        _DefaultIcon.enabled = false;
        _IconA.enabled = false;
        _IconB.enabled = false;
        _IconC.enabled = false;
        
        switch (matchType)
        {
            case BlockMatchGroupType.Default:
                _DefaultIcon.enabled = true;
                break;
            
            case BlockMatchGroupType.A:
                _IconA.enabled = true;
                break;
            
            case BlockMatchGroupType.B:
                _IconB.enabled = true;
                break;
            
            case BlockMatchGroupType.C:
                _IconC.enabled = true;
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(matchType), matchType, null);
        }
    }
}
