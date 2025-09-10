using OpenTK.Mathematics;

namespace VoxelEngine.Core
{
    public class Camera
    {
        public Vector3 Position = new Vector3(0.0f, 10.0f, 20.0f);
        public Vector3 Front = -Vector3.UnitZ;
        public Vector3 Up = Vector3.UnitY;
        public Vector3 Right => Vector3.Cross(Front, Up);

        public Matrix4 GetViewMatrix()
        {
            return Matrix4.LookAt(Position, Position + Front, Up);
        }

        public Matrix4 GetProjectionMatrix()
        {
            return Matrix4.CreatePerspectiveFieldOfView(
                MathHelper.DegreesToRadians(60f),
                800f / 600f,
                0.1f,
                1000f
            );
        }
    }
}