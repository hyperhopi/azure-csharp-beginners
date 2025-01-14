﻿using GreetingService.Core.Entities;
using GreetingService.Core.Exceptions;
using GreetingService.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreetingService.Infrastructure.UserService
{
    public class SqlUserService : IUserService
    {
        private readonly GreetingDbContext _greetingDbContext;
        private readonly ILogger<SqlUserService> _logger;

        public SqlUserService(GreetingDbContext greetingDbContext, ILogger<SqlUserService> logger)
        {
            _greetingDbContext = greetingDbContext;
            _logger = logger;
        }

        public async Task CreateUserAsync(User user)
        {
            if (await _greetingDbContext.Users.AnyAsync(x => x.Email == user.Email && x.ApprovalStatus == UserApprovalStatus.Approved))
                return;

            var existingUnapprovedUser = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.Email == user.Email && x.ApprovalStatus != UserApprovalStatus.Approved);
            if (existingUnapprovedUser != null)
                _greetingDbContext.Users.Remove(existingUnapprovedUser);

            user.Created = DateTime.Now;
            user.Modified = DateTime.Now;
            user.ApprovalStatus = UserApprovalStatus.Pending;
            user.ApprovalStatusNote = "Awaiting approval from administrator";
            await _greetingDbContext.Users.AddAsync(user);
            await _greetingDbContext.SaveChangesAsync();
        }

        public async Task DeleteUserAsync(string email)
        {
            var user = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));
            if (user == null)
            {
                _logger.LogWarning("Delete user failed, user with email {email} not found", email);
                throw new UserNotFoundException($"User {email} not found");
            }

            _greetingDbContext.Users.Remove(user);
            await _greetingDbContext.SaveChangesAsync();
        }

        public async Task<User> GetUserAsync(string email)
        {
            var user = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.Email.Equals(email));
            if (user == null)
                return null;
            
            return user;
        }

        public async Task<IEnumerable<User>> GetUsersAsync()
        {
            var users = await _greetingDbContext.Users.ToListAsync();
            return users;
        }

        public async Task UpdateUserAsync(User user)
        {
            var existingUser = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.Email.Equals(user.Email));
            if (existingUser == null)
            {
                _logger.LogWarning("Update user failed, user with email {email} not found", user.Email);
                throw new UserNotFoundException($"User {user.Email} not found");
            }

            if (!string.IsNullOrWhiteSpace(user.Password))
                existingUser.Password = user.Password;

            if (!string.IsNullOrWhiteSpace(user.LastName))
                existingUser.LastName = user.LastName;

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                existingUser.FirstName = user.FirstName;

            existingUser.Modified = DateTime.Now;          
            await _greetingDbContext.SaveChangesAsync();
        }

        public async Task<bool> IsValidUserAsync(string username, string password)
        {
            var user = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.Email.Equals(username));
            if (user != null && user.Password.Equals(password))
                return true;

            return false;
        }

        public bool IsValidUser(string username, string password)
        {
            var user = _greetingDbContext.Users.FirstOrDefault(x => x.Email.Equals(username));
            if (user != null && user.Password.Equals(password))
                return true;

            return false;
        }

        public async Task ApproveUserAsync(string approvalCode)
        {
            User? user = await GetUserForApprovalAsync(approvalCode);

            user.ApprovalStatus = UserApprovalStatus.Approved;
            user.ApprovalStatusNote = $"Approved by an administrator at {DateTime.Now:O}";
            await _greetingDbContext.SaveChangesAsync();
        }

        public async Task RejectUserAsync(string approvalCode)
        {
            var user = await GetUserForApprovalAsync(approvalCode);

            user.ApprovalStatus = UserApprovalStatus.Rejected;
            user.ApprovalStatusNote = $"Rejected by an administrator at {DateTime.Now:O}";
            await _greetingDbContext.SaveChangesAsync();
        }

        private async Task<User> GetUserForApprovalAsync(string approvalCode)
        {
            var user = await _greetingDbContext.Users.FirstOrDefaultAsync(x => x.ApprovalStatus == UserApprovalStatus.Pending && x.ApprovalCode.Equals(approvalCode) && x.ApprovalExpiry > DateTime.Now);
            if (user == null)
                throw new UserNotFoundException($"User with approval code: {approvalCode} not found");
            
            return user;
        }
    }
}
