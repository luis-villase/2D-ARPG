using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class Hero : Character {
        private GameObject _gameOverScreen;
        protected internal int Score;
        public Camera GameCamera;
        public AudioClip GameOverClip;

        public override void Start() {

            base.Start();
            _gameOverScreen = transform.Find("Canvas").Find("GameOverScreen").gameObject;
            _gameOverScreen.SetActive(false);
        }

        public override void Update() {
            base.Update();
            if (Input.GetKeyUp(KeyCode.UpArrow)) {
                StartCoroutine(Attack(
                    Quaternion.Euler(0, 230, 0),
                    new Vector3(0, 0.25f, 0.75f), 90, Vector3.forward));
            }
            if (Input.GetKeyUp(KeyCode.DownArrow)) {
                StartCoroutine(Attack(
                    Quaternion.Euler(0, 50, 0),
                    new Vector3(0, 0.25f, -0.75f), 270, Vector3.back));
            }
            if (Input.GetKeyUp(KeyCode.LeftArrow)) {
                StartCoroutine(Attack(
                    Quaternion.Euler(0, 140, 0),
                    new Vector3(-0.75f, 0.25f, 0), 0, Vector3.left));
            }
            if (Input.GetKeyUp(KeyCode.RightArrow)) {
                StartCoroutine(Attack(
                    Quaternion.Euler(0, 320, 0),
                    new Vector3(0.75f, 0.25f, 0), 180, Vector3.right));
            }

            if (IsMoving) {
                return;
            }

            OriginPosition = transform.position;
            if (Input.GetKey(KeyCode.W) && CanMove(transform.position, Vector3.forward, true)) {
                StartCoroutine(Move(transform, Vector3.forward));
                StartCoroutine(Move(GameCamera.transform, Vector3.forward));
            }

            if (Input.GetKey(KeyCode.S) && CanMove(transform.position, Vector3.back, true)) {
                StartCoroutine(Move(transform, Vector3.back));
                StartCoroutine(Move(GameCamera.transform, Vector3.back));
            }

            if (Input.GetKey(KeyCode.A) && CanMove(transform.position, Vector3.left, true)) {
                StartCoroutine(Move(transform, Vector3.left));
                StartCoroutine(Move(GameCamera.transform, Vector3.left));
            }

            if (Input.GetKey(KeyCode.D) && CanMove(transform.position, Vector3.right, true)) {
                StartCoroutine(Move(transform, Vector3.right));
                StartCoroutine(Move(GameCamera.transform, Vector3.right));
            }

            if (Input.GetKey(KeyCode.P)) {
                SceneManager.LoadScene("Level", LoadSceneMode.Single);
            }
        }

        public void Quit() {
            Application.Quit();
        }

        public void Menu() {
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }

        public void Restart() {
            Time.timeScale = 1;
            SceneManager.LoadScene("Level", LoadSceneMode.Single);
        }

        protected internal void Die() {
            Time.timeScale = 0;
            CurrentHealth = 0;
            var score = transform.Find("Canvas").Find("GameOverScreen").Find("ScoreText").GetComponent<Text>();
            score.text = "Your Score: " + Score;
            var highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (highScore < Score) {
                PlayerPrefs.SetInt("HighScore", Score);
                PlayerPrefs.Save();
            }

            _gameOverScreen.SetActive(true);
            _gameOverScreen.transform.SetAsLastSibling();
            if (AudioSource == null) return;
            AudioSource.clip = GameOverClip;
            AudioSource.loop = true;
            AudioSource.Play();
        }

        public override void TakeDamage(int damage) {
            base.TakeDamage(damage);
            if (CurrentHealth - damage <= 0) {
                Die();
            }
            else {
                CurrentHealth -= damage;
                HealthBar.value = CurrentHealth / (float) Health;
            }
        }
    }
}