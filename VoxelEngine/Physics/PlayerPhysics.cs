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
            
            // Normal moda geçerken velocity'yi sıfırla ve ground state'i güncelle
            if (Mode == PlayerMode.Normal)
            {
                Velocity = Vector3.Zero;
                _isGrounded = IsGrounded();
            }
        }
        
        public void ApplyMovement(Vector3 movement, float deltaTime)
        {
            if (Mode == PlayerMode.God)
            {
                // God modda direkt hareket et
                Position += movement * deltaTime;
                return;
            }

            // Normal modda collision-aware hareket
            Vector3 horizontal = new Vector3(movement.X, 0, movement.Z);
            Vector3 deltaMove = horizontal * deltaTime;
            
            // X hareketi
            Vector3 newPosX = Position + new Vector3(deltaMove.X, 0, 0);
            if (!HasSimpleCollision(newPosX))
            {
                Position.X = newPosX.X;
            }
            
            // Z hareketi
            Vector3 newPosZ = Position + new Vector3(0, 0, deltaMove.Z);
            if (!HasSimpleCollision(newPosZ))
            {
                Position.Z = newPosZ.Z;
            }
        }
        
        public void Jump()
        {
            if (Mode == PlayerMode.Normal && _isGrounded)
            {
                Velocity.Y = 10.0f; // Zıplama hızı arttırıldı
                _isGrounded = false; // Anlık olarak havada işaretle
            }
        }

        public void Update(float deltaTime)
        {
            if (Mode == PlayerMode.Normal)
            {
                // Gravity 
                Velocity.Y -= 20.0f * deltaTime;
                
                // Y movement
                Position.Y += Velocity.Y * deltaTime;
                
                // Ground check - ayak seviyesinde
                Vector3 feetPos = new Vector3(Position.X, Position.Y - 1.6f, Position.Z);
                bool groundBelow = _world.GetBlock(feetPos) != BlockType.Air;
                
                if (groundBelow && Velocity.Y <= 0)
                {
                    // Snap to ground surface
                    int blockY = (int)Math.Floor(feetPos.Y);
                    Position.Y = blockY + 1 + 1.6f; // Block top + player height
                    Velocity.Y = 0;
                    _isGrounded = true;
                }
                else if (Velocity.Y > 0.1f)
                {
                    // Jumping/rising - not grounded
                    _isGrounded = false;
                }
                
                // Don't fall through world
                if (Position.Y < 10)
                {
                    Position.Y = 55;
                    Velocity.Y = 0;
                }
            }
        }
        
        private bool HasSimpleCollision(Vector3 position)
        {
            // Basit collision - player'in etrafındaki blokları kontrol et
            Vector3[] checkPoints = {
                position + new Vector3(-0.3f, -0.1f, -0.3f), // Alt sol ön
                position + new Vector3( 0.3f, -0.1f, -0.3f), // Alt sağ ön
                position + new Vector3(-0.3f, -0.1f,  0.3f), // Alt sol arka
                position + new Vector3( 0.3f, -0.1f,  0.3f), // Alt sağ arka
                position + new Vector3(-0.3f, -1.5f, -0.3f), // Ayak seviyesi
                position + new Vector3( 0.3f, -1.5f, -0.3f),
                position + new Vector3(-0.3f, -1.5f,  0.3f),
                position + new Vector3( 0.3f, -1.5f,  0.3f)
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
            // Basit zemin kontrolü
            Vector3 feetPos = new Vector3(Position.X, Position.Y - 1.82f, Position.Z);
            return _world.GetBlock(feetPos) != BlockType.Air;
        }
    }
}
