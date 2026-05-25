using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Networking.UI
{
    public class UIParticleSystem : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Sprite _particleSprite;
        [SerializeField] private Color _startColor = Color.white;
        [SerializeField] private Vector2 _spawnArea = new Vector2(100, 100);
        [SerializeField] private int _maxParticles = 50;
        [SerializeField] private float _emissionRate = 5f;
        [SerializeField] private Vector2 _sizeRange = new Vector2(20, 40);
        [SerializeField] private Vector2 _velocityRange = new Vector2(50, 100);
        [SerializeField] private Vector2 _gravity = new Vector2(0, -100);
        [SerializeField] private Vector2 _lifetimeRange = new Vector2(2f, 4f);
        [SerializeField] private bool _playOnAwake = true;

        private class Particle
        {
            public Image Image;
            public Vector2 Velocity;
            public float Lifetime;
            public float RemainingLifetime;
            public RectTransform Rect;
        }

        private List<Particle> _activeParticles = new List<Particle>();
        private Stack<Image> _pool = new Stack<Image>();
        private float _nextEmissionTime;
        private bool _isPlaying;

        private void Awake()
        {
            if (_playOnAwake) Play();
        }

        public void Play()
        {
            _isPlaying = true;
            _nextEmissionTime = Time.time;
            Debug.Log($"[UIParticleSystem] {gameObject.name} Started Playing.");
        }

        public void Stop()
        {
            _isPlaying = false;
            Debug.Log($"[UIParticleSystem] {gameObject.name} Stopped Playing.");
        }

        public void Clear()
        {
            foreach (var p in _activeParticles)
            {
                if (p.Image != null)
                {
                    p.Image.gameObject.SetActive(false);
                    _pool.Push(p.Image);
                }
            }
            _activeParticles.Clear();
            Debug.Log($"[UIParticleSystem] {gameObject.name} Cleared.");
        }

        private void Update()
        {
            if (_isPlaying && Time.time >= _nextEmissionTime && _activeParticles.Count < _maxParticles)
            {
                Emit();
                _nextEmissionTime = Time.time + (1f / _emissionRate);
            }

            for (int i = _activeParticles.Count - 1; i >= 0; i--)
            {
                var p = _activeParticles[i];
                p.RemainingLifetime -= Time.deltaTime;

                if (p.RemainingLifetime <= 0)
                {
                    p.Image.gameObject.SetActive(false);
                    _pool.Push(p.Image);
                    _activeParticles.RemoveAt(i);
                    continue;
                }

                p.Velocity += _gravity * Time.deltaTime;
                p.Rect.anchoredPosition += p.Velocity * Time.deltaTime;
                
                // Fade out
                Color c = _startColor;
                c.a *= (p.RemainingLifetime / p.Lifetime);
                p.Image.color = c;
            }
        }

        private void Emit()
        {
            Image img;
            if (_pool.Count > 0)
            {
                img = _pool.Pop();
                img.gameObject.SetActive(true);
            }
            else
            {
                GameObject go = new GameObject("Particle", typeof(Image));
                go.transform.SetParent(transform, false);
                img = go.GetComponent<Image>();
                img.raycastTarget = false;
            }

            img.sprite = _particleSprite;
            img.color = _startColor;

            RectTransform rect = img.rectTransform;
            rect.anchoredPosition = new Vector2(
                Random.Range(-_spawnArea.x / 2f, _spawnArea.x / 2f),
                Random.Range(-_spawnArea.y / 2f, _spawnArea.y / 2f)
            );

            float size = Random.Range(_sizeRange.x, _sizeRange.y);
            rect.sizeDelta = new Vector2(size, size);

            float lifetime = Random.Range(_lifetimeRange.x, _lifetimeRange.y);
            
            _activeParticles.Add(new Particle
            {
                Image = img,
                Rect = rect,
                Velocity = new Vector2(0, -Random.Range(_velocityRange.x, _velocityRange.y)),
                Lifetime = lifetime,
                RemainingLifetime = lifetime
            });
        }
    }
}
