using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace Assets.Scripts {
    public enum CameraPosition {
        Center,
        North,
        South,
        East,
        West
    }

    public enum Difficulty {
        VeryEasy = 0,
        Easy = 1,
        Normal = 2,
        Hard = 3,
        SuperHard = 4
    }

    public enum CharacterType {
        Player = -1,
        Melee = 0,
        Tank = 1,
        Ranged = 2,
        Other = 4
    }

    public delegate bool MovementDelegate(Vector3 position, Vector3 direction, bool isPlayer);
    public delegate bool AttackDelegate(Vector3 position, Vector3 direction, bool isPlayer, int damage);
    public delegate void KillEnemyDelegate(GameObject enemy);

    public class MapGenerator : MonoBehaviour {
        private int _halfHeight;
        private int _halfWidth;
        private int _height;
        private int[,] _tileSet;
        private int _width;
        private Hero _heroObject;
        private readonly HashSet<GameObject> _enemies = new HashSet<GameObject>();
        private readonly int[,] _enemyDistribution = {
            { 60, 80, 90 },
            { 40, 70, 88 },
            { 35, 65, 85 },
            { 23, 58, 83 },
            { 10, 50, 70 }
        };

        public static GameObject Hero;

        [Header("Map Config")] 
        public int MapSaturation;
        public int VonNeumannPasses;
        public int MaxWidth;
        public int MaxHeight;
        public GameObject[] FloorTiles;
        public GameObject[] WallTiles;

        [Header("Camera")]
        public Camera GameCamera;
        public int CameraZoom;
        public CameraPosition CameraLocation;

        [Header("Enemies")]
        public int EnemySaturation;
        public Difficulty Difficulty;
        public GameObject[] EnemyTypes;

        private void Start() {
            _width = Random.Range(30, MaxWidth);
            _height = Random.Range(30, MaxHeight);
            _halfWidth = _width / 2;
            _halfHeight = _width / 2;

            CreateTileSet();
            DisplayTileSet();
            var cameraX = 0;
            var cameraY = 0;

            var cameraLocationString = PlayerPrefs.GetString("StartingPosition", "West");
            CameraLocation = (CameraPosition) Enum.Parse(typeof(CameraPosition), cameraLocationString);
            switch (CameraLocation) {
                case CameraPosition.Center:
                    cameraX = _halfWidth;
                    cameraY = _halfHeight;
                    break;
                case CameraPosition.South:
                    cameraX = _halfWidth;
                    cameraY = 0;
                    break;
                case CameraPosition.East:
                    cameraX = _width - 1;
                    cameraY = _halfHeight;
                    break;
                case CameraPosition.North:
                    cameraX = _halfWidth;
                    cameraY = _height - 1;
                    break;
                case CameraPosition.West:
                    cameraX = 0;
                    cameraY = _halfHeight;
                    break;
            }

            PlayerPrefs.DeleteKey("StartingPosition");

            GameCamera.transform.position = new Vector3(cameraX, CameraZoom, cameraY);

            var playerClass = PlayerPrefs.GetString("Hero", "Barbarian");
            Hero = Instantiate(playerClass == "Barbarian" ? transform.Find("Barbarian").gameObject : transform.Find("Amazon").gameObject);
            Hero.SetActive(Hero);
            Hero.transform.position = new Vector3(cameraX, 0.75f, cameraY);
            _heroObject = Hero.GetComponent<Hero>();
            _heroObject.CanMove = CanCharacterMove;
            _heroObject.HitAndDamage = HitAndDamage;
            _heroObject.GameCamera = GameCamera;
            var audioSource = GetComponent<AudioSource>();
            audioSource.Play();
        }

        private void Update() {
            if (_heroObject.CurrentHealth <= 0) {
                GetComponent<AudioSource>().Stop();
            }
        }

        private void CreateTileSet() {
            try {
                _tileSet = new int[_width, _height];
                for (var i = 0; i < _height * _width; i++) {
                    var xPosition = i % _width;
                    var yPosition = (i - xPosition) / _width;
                    //Setting walls all around the map, so that you can only exit at the gates
                    if (xPosition == 0 || yPosition == 0 || xPosition == _width - 1 || yPosition == _height - 1) {
                        _tileSet[xPosition, yPosition] = 1;
                    } else {
                        //Adding random blocks for seeding our cellular automaton
                        _tileSet[xPosition, yPosition] = Random.Range(0, 100) < MapSaturation ? 1 : 0;
                    }
                }
                PopulateMap();

                // Setting the gates, this looks ugly, but it keeps us on constant time
                for (var i = _halfWidth - 2; i <= _halfWidth + 1; i++) {
                    _tileSet[i, 0] = 0;
                    _tileSet[i, 1] = 0;
                    _tileSet[i, 2] = 0;
                    _tileSet[i, 3] = 0;
                    _tileSet[i, _height - 1] = 0;
                    _tileSet[i, _height - 1] = 0;
                    _tileSet[i, _height - 1] = 0;
                    _tileSet[i, _height - 1] = 0;
                }

                for (var i = _halfHeight - 2; i <= _halfHeight + 1; i++) {
                    _tileSet[0, i] = 0;
                    _tileSet[1, i] = 0;
                    _tileSet[2, i] = 0;
                    _tileSet[3, i] = 0;
                    _tileSet[_width - 1, i] = 0;
                    _tileSet[_width - 1, i] = 0;
                    _tileSet[_width - 1, i] = 0;
                    _tileSet[_width - 1, i] = 0;
                }


            } catch (Exception e) {
                Debug.Log(e);
                Debug.Log(_width + "x" + _height);
            }
        }

        private void PopulateMap() {
            //Run Von Neumann neighborhood to smooth out and finish the cellular automaton
            for (var pass = 1; pass <= VonNeumannPasses; pass++) {
                for (var i = 0; i < _height * _width; i++) {
                    var xPosition = i % _width;
                    var yPosition = (i - xPosition) / _width;
                    //don't mess with the walls
                    if (xPosition == 0 || yPosition == 0 || xPosition == _width - 1 ||
                        yPosition == _height - 1) continue;
                    var neighborTiles = _tileSet[xPosition - 1, yPosition] + _tileSet[xPosition + 1, yPosition] +
                                        _tileSet[xPosition, yPosition - 1] + _tileSet[xPosition, yPosition + 1];
                    if (neighborTiles > 2) {
                        _tileSet[xPosition, yPosition] = 1;
                    }
                    else if (neighborTiles < 2) {
                        _tileSet[xPosition, yPosition] = 0;
                        if (pass == VonNeumannPasses) {
                            AddEnemy(xPosition, yPosition);
                        }
                    }
                }
            }
        }


        private void DisplayTileSet() {
            var theme = Random.Range(0, FloorTiles.Length);
            for (var i = 0; i < _height * _width; i++) {
                var xPosition = i % _width;
                var yPosition = (i - xPosition) / _width;

                if (_tileSet[xPosition, yPosition] == 0) {
                    Instantiate(FloorTiles[theme],
                        new Vector3(xPosition, 0.25f, yPosition), Quaternion.identity);
                }
                else {
                    Instantiate(WallTiles[theme],
                        new Vector3(xPosition, 1, yPosition), Quaternion.identity);
                }
            }
        }

        private void AddEnemy(int xPosition, int yPosition) {
            if (_tileSet[xPosition, yPosition] == 0 && Random.Range(0, 100) < EnemySaturation) {
                var newEnemy = Instantiate(CalculateEnemyDistribution(),
                    new Vector3(xPosition, 0.75f, yPosition), Quaternion.identity);
                newEnemy.GetComponent<Enemy>().CanMove = CanCharacterMove;
                newEnemy.GetComponent<Enemy>().KillEnemy = KillEnemy;
                newEnemy.GetComponent<Enemy>().HitAndDamage = HitAndDamage;
                _enemies.Add(newEnemy);
            }
        }

        private GameObject CalculateEnemyDistribution() {
            var lookup = Random.Range(0, 100);
            for (var i = 0; i < 3; i++) {
                if (lookup <= _enemyDistribution[(int) Difficulty, i]) {
                    return EnemyTypes[i];
                }
            }

            return EnemyTypes.Last();
        }

        private bool CanCharacterMove(Vector3 position, Vector3 direction, bool isPlayer) {
            if (direction == Vector3.forward) return IsValidToMove((int) position.x, (int) position.z + 1, isPlayer);

            if (direction == Vector3.back) return IsValidToMove((int) position.x, (int) position.z - 1, isPlayer);

            if (direction == Vector3.left) return IsValidToMove((int) position.x - 1, (int) position.z, isPlayer);

            if (direction == Vector3.right) return IsValidToMove((int) position.x + 1, (int) position.z, isPlayer);

            return false;
        }

        private bool IsValidToMove(int x, int y, bool isPlayer) {
            if (isPlayer && x < 0) {
                PlayerPrefs.SetString("StartingPosition", "East");
                SceneManager.LoadScene("Level", LoadSceneMode.Single);
            } else if (isPlayer && x >= _width) {
                PlayerPrefs.SetString("StartingPosition", "West");
                SceneManager.LoadScene("Level", LoadSceneMode.Single);
            } else if (isPlayer && y < 0) {
                PlayerPrefs.SetString("StartingPosition", "North");
                SceneManager.LoadScene("Level", LoadSceneMode.Single);
            } else if (isPlayer && y >= _height) {
                PlayerPrefs.SetString("StartingPosition", "South");
                SceneManager.LoadScene("Level", LoadSceneMode.Single);
            }

            if (x < 0 || x >= _width ||
                y < 0 || y >= _height) {
                return false;
            }

            return _tileSet[x, y] == 0 && CheckCollision(x, y, isPlayer) == null;
        }

        private bool HitAndDamage(Vector3 position, Vector3 direction, bool isPlayer, int damage) {
            GameObject collided = null;
            if (direction == Vector3.forward) {
                collided = CheckCollision((int) position.x, (int) position.z + 1, isPlayer);
            }
            if (direction == Vector3.back) {
                collided = CheckCollision((int) position.x, (int) position.z - 1, isPlayer);
            }
            if (direction == Vector3.left) {
                collided = CheckCollision((int) position.x - 1, (int) position.z, isPlayer);
            }
            if (direction == Vector3.right) {
                collided = CheckCollision((int) position.x + 1, (int) position.z, isPlayer);
            }

            if (collided == null) {
                return false;
            }
            
            collided.GetComponent<Character>().TakeDamage(damage);
            return true;
        }

        private GameObject CheckCollision(int x, int y, bool isPlayer) {
            if (_enemies.Any(e => (int)e.GetComponent<Enemy>().TargetLocation.x == x &&
                                  (int)e.GetComponent<Enemy>().TargetLocation.z == y)) {
                return _enemies.First(e => (int) e.GetComponent<Enemy>().TargetLocation.x == x &&
                                           (int) e.GetComponent<Enemy>().TargetLocation.z == y);
            }
            if (!isPlayer && (int) Hero.GetComponent<Hero>().TargetLocation.x == x &&
                (int) Hero.GetComponent<Hero>().TargetLocation.z == y) {
                return Hero;
            }
            return null;
        }

        private void KillEnemy(GameObject enemy) {
            _heroObject.Score += 1;
            _enemies.Remove(enemy);
            Destroy(enemy);
        }
    }
}
