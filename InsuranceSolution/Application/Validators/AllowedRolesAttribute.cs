using Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Validators
{
    public class AllowedRolesAttribute : ValidationAttribute
    {
        private readonly UserRole[] _allowedRoles;

        public AllowedRolesAttribute(params UserRole[] allowedRoles)
        {
            _allowedRoles = allowedRoles;
        }

        protected override ValidationResult? IsValid(
            object? value, ValidationContext context)
        {
            if (value is UserRole role && _allowedRoles.Contains(role))
                return ValidationResult.Success;

            return new ValidationResult(
                $"Role must be one of: {string.Join(", ", _allowedRoles)}");
        }
    }
}
