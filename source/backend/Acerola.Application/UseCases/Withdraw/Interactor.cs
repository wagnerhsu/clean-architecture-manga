﻿namespace Acerola.Application.UseCases.Withdraw
{
    using System.Threading.Tasks;
    using Acerola.Domain.Accounts;
    using Acerola.Domain.ValueObjects;

    public class Interactor : IInputBoundary<Request>
    {
        private readonly IAccountReadOnlyRepository accountReadOnlyRepository;
        private readonly IAccountWriteOnlyRepository accountWriteOnlyRepository;
        private readonly IOutputBoundary<Response> outputBoundary;

        public Interactor(
            IAccountReadOnlyRepository accountReadOnlyRepository,
            IAccountWriteOnlyRepository accountWriteOnlyRepository,
            IOutputBoundary<Response> outputBoundary)
        {
            this.accountReadOnlyRepository = accountReadOnlyRepository;
            this.accountWriteOnlyRepository = accountWriteOnlyRepository;
            this.outputBoundary = outputBoundary;
        }

        public async Task Handle(Request request)
        {
            Account account = await accountReadOnlyRepository.Get(request.AccountId);
            if (account == null)
                throw new AccountNotFoundException($"The account {request.AccountId} does not exists or is already closed.");

            Debit debit = Debit.Create(Amount.Create(request.Amount));
            account.Withdraw(debit);

            await accountWriteOnlyRepository.Update(account);

            Account updatedAccount = await accountReadOnlyRepository.Get(request.AccountId);
            if (updatedAccount == null)
                throw new AccountNotFoundException($"The account {request.AccountId} does not exists or is already closed.");

            outputBoundary.Populate(
                new Response(
                    debit.Amount.Value, 
                    updatedAccount.CurrentBalance.Value, 
                    debit.Description,
                    debit.TransactionDate
                ));
        }
    }
}