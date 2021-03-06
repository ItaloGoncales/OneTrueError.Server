﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using OneTrueError.App.Core.Accounts;
using Xunit;

namespace OneTrueError.App.Tests.Core.Accounts
{

    public class AccountTests
    {
        [Fact]
        public void should_fail_if_account_have_not_been_activated()
        {
            var sut = new Account("arne", "kalle");

            Action actual = () => sut.Login("ping");

            actual.ShouldThrow<AuthenticationException>();
        }

        [Fact]
        public void should_be_able_to_activate_the_account()
        {
            var sut = new Account("arne", "kalle");

            sut.Activate();
            var actual = sut.Login("kalle");

            actual.Should().BeTrue();
        }

        [Fact]
        public void three_failed_login_attempts_should_lock_the_account()
        {
            var sut = new Account("arne", "kalle");
            sut.Activate();

            sut.Login("ping");
            sut.Login("pong");
            Action actual = () => sut.Login("ping");

            actual.ShouldThrow<AuthenticationException>();
            sut.AccountState.Should().Be(AccountState.Locked);
        }

        [Fact]
        public void should_generate_activation_key_when_reset_is_requested_so_that_the_user_can_activate_its_account_user_the_emailed_link()
        {
            var sut = new Account("arne", "kalle");
            sut.Activate();
            sut.Login("ping");
            sut.Login("pong");
            try { sut.Login("ping");} catch { }

            sut.RequestPasswordReset();

            sut.ActivationKey.Should().NotBeNull();
            sut.AccountState.Should().Be(AccountState.ResetPassword);
        }

        [Fact]
        public void should_be_able_to_change_password()
        {
            var sut = new Account("arne", "kalle");

            sut.ChangePassword("new");
            var actual = sut.Login("new");

            actual.Should().BeTrue();
        }

        [Fact]
        public void should_be_able_to_validate_the_current_password_so_that_We_know_that_its_safe_to_change_it()
        {
            var sut = new Account("arne", "kalle");

            var actual = sut.ValidatePassword("kalle");

            actual.Should().BeTrue();
        }

        [Fact]
        public void validate_should_not_accept_invalid_password()
        {
            var sut = new Account("arne", "kalle");

            var actual = sut.ValidatePassword("kall");

            actual.Should().BeFalse();
        }


    }
}
