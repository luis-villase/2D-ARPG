using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts {
    public class Enemy : Character {
        private readonly Vector3[] _directions = {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right
        };

        private Transform _player;
        private Hero _playerClass;
        private TextMesh _text;
        public HashSet<Transform> Arrows = new HashSet<Transform>();
        public int FoV;
        public KillEnemyDelegate KillEnemy;
        public Material[] Skins;
        public CharacterType Type;

        public override void Start() {
            base.Start();
            _player = MapGenerator.Hero.transform;
            _playerClass = MapGenerator.Hero.GetComponent<Hero>();
            _text = transform.GetChild(1).GetComponent<TextMesh>();
            var skinToUse = Random.Range(0, Skins.Length);
            GetComponent<Renderer>().material = Skins[skinToUse];
        }

        public override void Update() {
            base.Update();
            if (_playerClass.CurrentHealth <= 0) return;
            var canSeePlayer = CanSeePlayer();
            var canAttackPlayer = CanAttackPlayer();

            if (!canSeePlayer) {
                Patrol();
            } else if (!canAttackPlayer) {
                Chase();
            } else {
                Attack();
            }

            if (!HasRangedAttack || !Arrows.Any()) return;
            
            foreach (var arrow in Arrows.Where(arrow => arrow != null && (int)arrow.position.x == (int)_player.position.x &&
                                                        (int)arrow.position.z == (int)_player.position.z).ToList()) {
                _playerClass.TakeDamage(Damage);
                Arrows.Remove(arrow);
                Destroy(arrow.gameObject);
            }
        }

        private void Patrol() {
            _text.text = "Patrol";
            if (IsMoving) return;
            var seed = Random.Range(0, 4);
            var direction = _directions[seed];
            if (CanMove(transform.position, direction, false)) {
                StartCoroutine(Move(transform, direction));
            }
        }

        private void Chase() {
            _text.text = "Chase";
            if (IsMoving) return;
            var distance = Vector3.Distance(_player.position, transform.position);
            if (distance <= 1) return;

            var position = _player.position - transform.position;

            Vector3 direction;
            if ((int) position.z > 0) {
                direction = Vector3.forward;
            } else if ((int) position.z < 0) {
                direction = Vector3.back;
            } else if ((int) position.x < 0) {
                direction = Vector3.left;
            } else {
                direction = Vector3.right;
            }

            if (CanMove(transform.position, direction, false)) {
                StartCoroutine(Move(transform, direction));
            }
        }

        private void Attack() {
            _text.text = "Wait";
            if (IsAttacking) return;
            _text.text = "Attack";
            var position = _player.position - transform.position;
            Quaternion startingRotation;
            Vector3 startingPosition;
            Vector3 direction;
            int targetAngle;
            if ((int) position.z > 0) {
                startingRotation = Quaternion.Euler(0, 230, 0);
                startingPosition = new Vector3(0, 0.25f, 0.75f);
                targetAngle = 90;
                direction = Vector3.forward;
            } else if ((int) position.z < 0) {
                startingRotation = Quaternion.Euler(0, 50, 0);
                startingPosition = new Vector3(0, 0.25f, -0.75f);
                targetAngle = 270;
                direction = Vector3.back;
            } else if ((int) position.x < 0) {
                startingRotation = Quaternion.Euler(0, 140, 0);
                startingPosition = new Vector3(-0.75f, 0.25f, 0);
                targetAngle = 0;
                direction = Vector3.left;
            } else {
                startingRotation = Quaternion.Euler(0, 320, 0);
                startingPosition = new Vector3(0.75f, 0.25f, 0);
                targetAngle = 180;
                direction = Vector3.right;
            }

            StartCoroutine(HasRangedAttack
                ? RangedAttack(_player, startingPosition + transform.position)
                : Attack(startingRotation, startingPosition, targetAngle, direction));
        }

        private void Heal() {
        }

        private bool CanSeePlayer() {
            return Vector3.Distance(_player.position, transform.position) <= FoV;
        }

        private bool CanAttackPlayer() {
            return Vector3.Distance(_player.position, transform.position) <= Range;
        }

        public override void TakeDamage(int damage) {
            base.TakeDamage(damage);
            if (CurrentHealth - damage <= 0) {
                KillEnemy(gameObject);
            } else {
                CurrentHealth -= damage;
                HealthBar.value = CurrentHealth / (float) Health;
            }
        }
    }
}