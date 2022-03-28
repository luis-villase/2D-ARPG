using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class Menu : MonoBehaviour {
        private Text _highScore;

        // Start is called before the first frame update
        private void Start() {
            _highScore = transform.Find("HighScore").GetComponent<Text>();
            _highScore.text = "High Score: " + PlayerPrefs.GetInt("HighScore", 0);
        }

        // Update is called once per frame
        private void Update() {
        }

        public void Quit() {
            Application.Quit();
        }

        public void Start(string characterClass) {
            PlayerPrefs.SetString("Hero", characterClass);
            SceneManager.LoadSceneAsync("Level", LoadSceneMode.Single);
            Destroy(gameObject);
        }

    }
}