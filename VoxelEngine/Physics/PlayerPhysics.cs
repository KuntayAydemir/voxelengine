using System;
using OpenTK.Mathematics;
using VoxelEngine.World;

namespace VoxelEngine.Physics
{
    public enum PlayerMode
    {
        God,    // Uçabilir, collision yok
        Normal  // Gravity var, collision var
    }

    public class PlayerPhysics
    {
        private GameWorld _world;
        public Vector3 Position = new Vector3(0, 52, 0); // Düz dünyanın hemen üstünde başla
        public Vector3 Velocity = Vector3.Zero;
        private bool _isGrounded;
        
        public PlayerMode Mode { get; private set; } = PlayerMode.God;
        
        public PlayerPhysics(GameWorld world)
        {
            _world = world;
        }
        
        public void ToggleMode()
        {
            Mode = Mode == PlayerMode.God ? PlayerMode.Normal : PlayerMode.God;
            
            // Normal moda geçerken velocity'yi sıfırla
            if (Mode == PlayerMode.Normal)
            {
                Velocity = Vector3.Zero;
            }
        }
        
        public void ApplyMovement(Vector3 movement, float deltaTime)
        {
            if (Mode == PlayerMode.God)
            {
                // God modda direkt hareket et
                Position += movement * deltaTime;
            }
            else
            {
                // Normal modda sadece yatay hareket, dikey hareket gravity tarafından kontrol edilir
                Vector3 horizontalMovement = new Vector3(movement.X, 0, movement.Z);
                Vector3 newPos = Position + horizontalMovement * deltaTime;
                
                // Collision check (basit)
                if (!HasCollision(newPos))
                {
                    Position.X = newPos.X;
                    Position.Z = newPos.Z;
                }
            }
        }
        
        public void Jump()
        {
            if (Mode == PlayerMode.Normal && _isGrounded)
            {
                Velocity.Y = 8.0f; // Zıplama hızı
            }
        }

        public void Update(float deltaTime)
        {
            if (Mode == PlayerMode.Normal)
            {
                // Yerçekimi
                if (!_isGrounded)
                {
                    Velocity.Y -= 25.0f * deltaTime; // Yerçekimi
                }

                // Zemin kontrolü
                _isGrounded = IsGrounded();

                // Zemine çarpınca düşüşü durdur
                if (_isGrounded && Velocity.Y < 0)
                {
                    Position.Y = (float)Math.Floor(Position.Y) + 1.8f; // Oyuncu yüksekliği
                    Velocity.Y = 0;
                }
                else
                {
                    // Dikey hareket uygula
                    Position.Y += Velocity.Y * deltaTime;
                }

                // Basit zemin sınırlaması
                if (Position.Y < 0)
                {
                    Position.Y = 0;
                    Velocity.Y = 0;
                    _isGrounded = true;
                }
            }
            // God modda hiçbir fizik uygulanmaz
        }
        
        private bool HasCollision(Vector3 position)
        {
            // Oyuncunun başı, göv desi ve ayakları için kontroller
            Vector3[] checkPoints = {
                position + new Vector3(0, 0, 0),      // Ayaklar
                position + new Vector3(0, 0.9f, 0),   // Gövde
                position + new Vector3(0, 1.7f, 0),   // Baş
            };
            
            foreach (var point in checkPoints)
            {
                if (_world.GetBlock(point) != BlockType.Air)
                    return true;
            }
            return false;
        }

        private bool IsGrounded()
        {
            Vector3 feetPosition = Position;
            feetPosition.Y -= 1.9f; // Oyuncu boy: 1.8, biraz aşağısında kontrol et
            return _world.GetBlock(feetPosition) != BlockType.Air;
        }
    }
}
