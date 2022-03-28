using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts {
    public class Character : MonoBehaviour {
        private bool _isEnemy;
        private int _targetAngle;
        private GameObject _projectile;
        private Vector3 _rangedTarget;
        protected Slider HealthBar;
        protected internal bool IsAttacking;
        protected internal bool IsMoving;
        protected internal int CurrentHealth;
        protected internal Vector3 OriginPosition;
        protected internal Vector3 TargetLocation;
        protected internal AudioSource AudioSource;
        public int Range;
        public float Speed;
        public GameObject Weapon;
        public AudioClip WeaponSound;
        public int AttackRate;
        public MovementDelegate CanMove;
        public AttackDelegate HitAndDamage;
        public int Damage;
        public AudioClip GruntSound;
        public bool HasRangedAttack;
        public int Health;

        public bool MarkedForDeath { get; private set; }

        public virtual void TakeDamage(int damage) {
            if (AudioSource == null) return;
            AudioSource.clip = GruntSound;
            AudioSource.Play();
        }

        public virtual void Start() {
            MarkedForDeath = false;
            Weapon.SetActive(false);
            AudioSource = GetComponent<AudioSource>();
            CurrentHealth = Health;
            _isEnemy = GetComponent<Hero>() == null;
            HealthBar = transform.Find("Canvas").Find("Slider").GetComponent<Slider>();
            IsMoving = false;
        }

        public virtual void Update()  {
            var wantedPosition = Camera.main.WorldToScreenPoint(transform.position + new Vector3(0, 0.25f, 0.6f));
            HealthBar.transform.position = wantedPosition;

            if (IsAttacking) {
                if (HasRangedAttack) {
                } else {
                    Weapon.transform.Rotate(Vector3.up * (2 * AttackRate));
                    if (Weapon.transform.rotation.eulerAngles.y <= _targetAngle + 10 &&
                        Weapon.GetComponent<Rigidbody>().transform.rotation.eulerAngles.y >= _targetAngle) {
                        IsAttacking = false;
                        Weapon.SetActive(false);
                    }
                }
            }
        }

        void FixedUpdate() {
            if (_projectile != null) {
                _projectile.GetComponent<Rigidbody>().AddForce(_rangedTarget * 4);
            }
        }

        internal IEnumerator Move(Transform objectTransform, Vector3 direction) {
            IsMoving = true;
            float elapsedTime = 0;
            var originalPosition = objectTransform.position;
            var targetPosition = originalPosition + direction;
            TargetLocation = originalPosition + direction;

            while (elapsedTime < Speed) {
                objectTransform.position =
                    Vector3.Lerp(originalPosition, targetPosition, elapsedTime / Speed);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            objectTransform.position = targetPosition;

            IsMoving = false;
        }

        protected internal IEnumerator Attack(Quaternion startingRotation, Vector3 position, int targetAngle, Vector3 direction) {
            Weapon.transform.rotation = startingRotation;
            Weapon.transform.position = transform.position + position;
            _targetAngle = targetAngle;

            AudioSource.clip = WeaponSound;
            AudioSource.Play();

            Weapon.SetActive(true);
            IsAttacking = true;
            HitAndDamage(transform.position, direction, !_isEnemy, Damage);
            yield return new WaitForSecondsRealtime(10 - AttackRate);
            IsAttacking = false;
        }
        protected internal IEnumerator RangedAttack(Transform target, Vector3 position) {
            _projectile = Instantiate(Weapon, position, Quaternion.identity, transform);
            _projectile.SetActive(true);
            _projectile.transform.LookAt(target);
            GetComponent<Enemy>().Arrows.Add(_projectile.transform);
            var heading = target.transform.position - _projectile.transform.position;
            _rangedTarget = heading / heading.magnitude;
            _projectile.GetComponent<Rigidbody>().AddForce(_rangedTarget * 1);
            IsAttacking = true;
            yield return new WaitForSecondsRealtime(6 - AttackRate);
            IsAttacking = false;
        }
    }
}