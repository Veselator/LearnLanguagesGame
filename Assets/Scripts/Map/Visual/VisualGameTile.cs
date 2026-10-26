using LearnLanguages.Map.Data;
using LearnLanguages.Map.Logic;
using TMPro;
using UnityEngine;
using Zenject;

namespace LearnLanguages.Map.Visual
{
    /// <summary>
    /// Visual of a province with a settlement: settlement icon, owner flag, port and name.
    /// </summary>
    public sealed class VisualGameTile : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _settlementRenderer;
        [SerializeField] private Sprite _villageSprite;
        [SerializeField] private Sprite _castleSprite;
        [SerializeField] private Sprite _citySprite;
        [SerializeField] private Transform _port;
        [SerializeField] private TMP_Text _nameLabel;

        private GameTile _tile;

        public void Bind(GameTile tile, Vector3 settlementPosition, Vector3 portPosition, Quaternion facing)
        {
            _tile = tile;
            transform.SetPositionAndRotation(settlementPosition, facing);
            _settlementRenderer.sprite = GetSprite(tile.Settlement);
            _port.SetPositionAndRotation(portPosition, facing);
            _nameLabel.text = tile.Data.Name;
            Refresh(Color.white, tile.IsNeutral);
        }

        public void Refresh(Color ownerColor, bool neutral)
        {
            _settlementRenderer.color = _tile.IsNeutral ? Color.white : ownerColor;
            _port.gameObject.SetActive(_tile.HasPort);
        }

        private Sprite GetSprite(SettlementType settlement)
        {
            switch (settlement)
            {
                case SettlementType.Castle: return _castleSprite;
                case SettlementType.City: return _citySprite;
                default: return _villageSprite;
            }
        }

        public sealed class Factory : PlaceholderFactory<VisualGameTile>
        {
        }
    }
}
