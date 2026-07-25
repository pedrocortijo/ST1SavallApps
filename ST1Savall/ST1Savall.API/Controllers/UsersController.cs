using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ST1Savall.API.Data;
using ST1Savall.Shared.Data;

namespace ST1Savall.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UsersController(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ApplicationUserDto>>> GetUsers()
    {
        var users = await _userManager.Users
            .OrderBy(u => u.Email ?? u.UserName)
            .Select(u => new ApplicationUserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                Tecnico = u.Tecnico,
                ClienteId = u.ClienteId,
                EmailConfirmed = u.EmailConfirmed,
                LockoutEnabled = u.LockoutEnabled,
                LockoutEnd = u.LockoutEnd
            })
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUserDto>> GetUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        return Ok(new ApplicationUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Tecnico = user.Tecnico,
            ClienteId = user.ClienteId,
            EmailConfirmed = user.EmailConfirmed,
            LockoutEnabled = user.LockoutEnabled,
            LockoutEnd = user.LockoutEnd
        });
    }

    [HttpPost]
    public async Task<ActionResult<ApplicationUserDto>> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { Message = "El correo electrónico y la contraseña son obligatorios." });

        var existingUser = await _userManager.FindByEmailAsync(dto.Email);
        if (existingUser != null)
            return BadRequest(new { Message = "Ya existe un usuario registrado con este correo electrónico." });

        var userName = string.IsNullOrWhiteSpace(dto.UserName) ? dto.Email : dto.UserName;

        var newUser = new ApplicationUser
        {
            UserName = userName,
            Email = dto.Email,
            EmailConfirmed = true,
            PhoneNumber = dto.PhoneNumber,
            Tecnico = dto.Tecnico,
            ClienteId = dto.ClienteId
        };

        var result = await _userManager.CreateAsync(newUser, dto.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"No se pudo crear el usuario: {errors}" });
        }

        return Ok(new ApplicationUserDto
        {
            Id = newUser.Id,
            UserName = newUser.UserName,
            Email = newUser.Email,
            PhoneNumber = newUser.PhoneNumber,
            Tecnico = newUser.Tecnico,
            ClienteId = newUser.ClienteId,
            EmailConfirmed = newUser.EmailConfirmed,
            LockoutEnabled = newUser.LockoutEnabled,
            LockoutEnd = newUser.LockoutEnd
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto dto)
    {
        if (id != dto.Id)
            return BadRequest(new { Message = "El ID del usuario no coincide." });

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        user.Email = dto.Email;
        user.UserName = string.IsNullOrWhiteSpace(dto.UserName) ? dto.Email : dto.UserName;
        user.PhoneNumber = dto.PhoneNumber;
        user.Tecnico = dto.Tecnico;
        user.ClienteId = dto.ClienteId;

        if (dto.Lockout)
        {
            user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
        }
        else
        {
            user.LockoutEnd = null;
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"Error al actualizar usuario: {errors}" });
        }

        return NoContent();
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserId) || string.IsNullOrWhiteSpace(dto.NewPassword))
            return BadRequest(new { Message = "Datos incompletos para el cambio de contraseña." });

        if (dto.NewPassword != dto.ConfirmPassword)
            return BadRequest(new { Message = "La nueva contraseña y su confirmación no coinciden." });

        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"No se pudo cambiar la contraseña: {errors}" });
        }

        return Ok(new { Message = "Contraseña actualizada correctamente." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
            return NotFound("Usuario no encontrado.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            return BadRequest(new { Message = $"No se pudo eliminar el usuario: {errors}" });
        }

        return NoContent();
    }
}
