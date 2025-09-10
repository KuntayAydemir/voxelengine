using OpenTK.Mathematics;
using VoxelEngine.World;

namespace VoxelEngine.Physics
{
    public class PlayerPhysics
    {
        private GameWorld _world;
        public Vector3 Position = new Vector3(0, 15, 0);
        public Vector3 Velocity = Vector3.Zero;
        private bool _isGrounded;

        public PlayerPhysics(GameWorld world)
        {
            _world = world;
        }

        public void Update(float deltaTime)
        {
            // Yerçekimi
            if (!_isGrounded)
            {
                Velocity.Y -= 20.0f * deltaTime; // Yerçekimi
            }

            // Zemin kontrolü
            _isGrounded = IsGrounded();

            // Zemine çarpınca düşüşü durdur
            if (_isGrounded && Velocity.Y < 0)
            {
                Velocity.Y = 0;
            }

            // Pozisyon güncelle
            Position += Velocity * deltaTime;

            // Basit zemin sınırlaması
            if (Position.Y < 0)
            {
                Position.Y = 0;
                Velocity.Y = 0;
                _isGrounded = true;
            }
        }

        private bool IsGrounded()
        {
            Vector3 feetPosition = Position;
            feetPosition.Y -= 0.5f; // Ayakların biraz aşağısında kontrol et
            return _world.GetBlock(feetPosition) != BlockType.Air;
        }
    }
}