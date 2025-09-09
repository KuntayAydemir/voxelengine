using System;
using OpenTK.Mathematics;
using VoxelEngine.World;

namespace VoxelEngine.Physics;

public class PlayerPhysics
{
    private readonly GameWorld _world; // World yerine GameWorld
    private readonly float _playerHeight = 1.8f;
    private readonly float _playerWidth = 0.6f;
    private readonly float _gravity = 9.81f;
    private Vector3 _velocity = Vector3.Zero;
    private bool _isOnGround = false;
    
    public PlayerPhysics(GameWorld world) // World yerine GameWorld
    {
        _world = world;
    }
    
    public void Update(ref Vector3 position, float deltaTime, bool jumpRequested)
    {
        // Apply gravity if not on ground
        if (!_isOnGround)
        {
            _velocity.Y -= _gravity * deltaTime;
        }
        else if (jumpRequested)
        {
            _velocity.Y = 5.0f; // Jump impulse
            _isOnGround = false;
        }
        
        // Apply velocity to position
        Vector3 newPosition = position + _velocity * deltaTime;
        
        // Check collisions and adjust position
        newPosition = HandleCollisions(position, newPosition);
        
        // Update position
        position = newPosition;
        
        // Check if player is on ground
        _isOnGround = IsOnGround(position);
        
        // Dampen horizontal velocity
        _velocity.X *= 0.9f;
        _velocity.Z *= 0.9f;
        
        // Cap velocity
        if (_velocity.Length > 20.0f)
        {
            _velocity = Vector3.Normalize(_velocity) * 20.0f;
        }
    }
    
    public void ApplyImpulse(Vector3 impulse)
    {
        _velocity += impulse;
    }
    
    private BlockType GetBlockAt(Vector3 position)
    {
        return _world.GetBlock(
            (int)Math.Floor(position.X), 
            (int)Math.Floor(position.Y), 
            (int)Math.Floor(position.Z)
        );
    }
    
    private Vector3 HandleCollisions(Vector3 oldPosition, Vector3 newPosition)
    {
        Vector3 result = newPosition;
        
        // Check for collisions at player's body
        for (float y = 0; y < _playerHeight; y += 0.5f)
        {
            CheckHorizontalCollisions(ref result, oldPosition, y);
        }
        
        // Check for vertical collisions
        if (newPosition.Y < oldPosition.Y) // Moving down
        {
            // Check for blocks below feet
            float feetY = result.Y;
            for (float x = -_playerWidth / 2; x <= _playerWidth / 2; x += _playerWidth / 2)
            {
                for (float z = -_playerWidth / 2; z <= _playerWidth / 2; z += _playerWidth / 2)
                {
                    Vector3 checkPos = new Vector3(result.X + x, feetY - 0.1f, result.Z + z);
                    if (GetBlockAt(checkPos) != BlockType.Air)
                    {
                        result.Y = (float)Math.Ceiling(checkPos.Y);
                        _velocity.Y = 0;
                        break;
                    }
                }
            }
        }
        else if (newPosition.Y > oldPosition.Y) // Moving up
        {
            // Check for blocks above head
            float headY = result.Y + _playerHeight;
            for (float x = -_playerWidth / 2; x <= _playerWidth / 2; x += _playerWidth / 2)
            {
                for (float z = -_playerWidth / 2; z <= _playerWidth / 2; z += _playerWidth / 2)
                {
                    Vector3 checkPos = new Vector3(result.X + x, headY + 0.1f, result.Z + z);
                    if (GetBlockAt(checkPos) != BlockType.Air)
                    {
                        result.Y = (float)Math.Floor(checkPos.Y) - _playerHeight;
                        _velocity.Y = 0;
                        break;
                    }
                }
            }
        }
        
        return result;
    }
    
    private void CheckHorizontalCollisions(ref Vector3 position, Vector3 oldPosition, float yOffset)
    {
        // Check X-axis
        if (position.X != oldPosition.X)
        {
            float checkX = position.X + (position.X > oldPosition.X ? _playerWidth / 2 : -_playerWidth / 2);
            for (float z = -_playerWidth / 2; z <= _playerWidth / 2; z += _playerWidth / 2)
            {
                Vector3 checkPos = new Vector3(checkX, position.Y + yOffset, position.Z + z);
                if (GetBlockAt(checkPos) != BlockType.Air)
                {
                    position.X = oldPosition.X;
                    _velocity.X = 0;
                    break;
                }
            }
        }
        
        // Check Z-axis
        if (position.Z != oldPosition.Z)
        {
            float checkZ = position.Z + (position.Z > oldPosition.Z ? _playerWidth / 2 : -_playerWidth / 2);
            for (float x = -_playerWidth / 2; x <= _playerWidth / 2; x += _playerWidth / 2)
            {
                Vector3 checkPos = new Vector3(position.X + x, position.Y + yOffset, checkZ);
                if (GetBlockAt(checkPos) != BlockType.Air)
                {
                    position.Z = oldPosition.Z;
                    _velocity.Z = 0;
                    break;
                }
            }
        }
    }
    
    private bool IsOnGround(Vector3 position)
    {
        // Check if there's a block directly below the player
        for (float x = -_playerWidth / 2; x <= _playerWidth / 2; x += _playerWidth / 2)
        {
            for (float z = -_playerWidth / 2; z <= _playerWidth / 2; z += _playerWidth / 2)
            {
                Vector3 checkPos = new Vector3(position.X + x, position.Y - 0.1f, position.Z + z);
                if (GetBlockAt(checkPos) != BlockType.Air)
                {
                    return true;
                }
            }
        }
        
        return false;
    }
}