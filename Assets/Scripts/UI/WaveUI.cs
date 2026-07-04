using TD.Waves;
using TMPro;
using UnityEngine;

namespace TD.UI
{
    public class WaveUI : MonoBehaviour
    {
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private TextMeshProUGUI waveText;
        [SerializeField] private TextMeshProUGUI stateText;
        [SerializeField] private TextMeshProUGUI remainingEnemyText;
        [SerializeField] private string waveFormat = "Wave: {0} / {1}";
        [SerializeField] private string remainingFormat = "Enemies: {0}";

        private void Awake()
        {
            if (waveText == null)
            {
                waveText = GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnEnable()
        {
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        public void SetWaveManager(WaveManager manager)
        {
            Unsubscribe();
            waveManager = manager;
            Subscribe();
            Refresh();
        }

        private void Subscribe()
        {
            if (waveManager == null)
            {
                return;
            }

            waveManager.OnWaveStarted.AddListener(HandleWaveNumberChanged);
            waveManager.OnWaveCleared.AddListener(HandleWaveNumberChanged);
            waveManager.OnRemainingEnemiesChanged.AddListener(UpdateRemainingEnemies);
            waveManager.OnWaveStateChanged.AddListener(UpdateState);
        }

        private void Unsubscribe()
        {
            if (waveManager == null)
            {
                return;
            }

            waveManager.OnWaveStarted.RemoveListener(HandleWaveNumberChanged);
            waveManager.OnWaveCleared.RemoveListener(HandleWaveNumberChanged);
            waveManager.OnRemainingEnemiesChanged.RemoveListener(UpdateRemainingEnemies);
            waveManager.OnWaveStateChanged.RemoveListener(UpdateState);
        }

        private void Refresh()
        {
            if (waveManager == null)
            {
                return;
            }

            UpdateWaveText();
            UpdateState(waveManager.CurrentState);
            UpdateRemainingEnemies(waveManager.AliveEnemyCount);
        }

        private void HandleWaveNumberChanged(int waveNumber)
        {
            UpdateWaveText();
        }

        private void UpdateWaveText()
        {
            if (waveText != null && waveManager != null)
            {
                waveText.text = string.Format(
                    waveFormat,
                    waveManager.CurrentWaveNumber,
                    waveManager.TotalWaveCount);
            }
        }

        private void UpdateState(string state)
        {
            if (stateText != null)
            {
                stateText.text = state;
            }
        }

        private void UpdateRemainingEnemies(int remaining)
        {
            if (remainingEnemyText != null)
            {
                remainingEnemyText.text = string.Format(remainingFormat, remaining);
            }
        }
    }
}
