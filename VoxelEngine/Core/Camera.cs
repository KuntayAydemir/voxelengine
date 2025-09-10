using OpenTK.Mathematics;

namespace VoxelEngine.Core
{
    public class Camera
    {
        public Vector3 Position = new Vector3(0.0f, 30.0f, 60.0f);
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right => Vector3.Normalize(Vector3.Cross(Front, Up));

        private float _yaw = -90.0f; // looking along -Z
        private float _pitch = 0.0f;

        public void YawPitch(float yawDelta, float pitchDelta)
        {
            _yaw += yawDelta;
            _pitch = MathHelper.Clamp(_pitch + pitchDelta, -89f, 89f);

            var front = new Vector3(
                MathF.Cos(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(_pitch)),
                MathF.Sin(MathHelper.DegreesToRadians(_yaw)) * MathF.Cos(MathHelper.DegreesToRadians(_pitch))
            );
            Front = Vector3.Normalize(front);
        }

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(70f),
                1920f / 1080f,
                0.1f,
                1000f
            );
        }
    }
}