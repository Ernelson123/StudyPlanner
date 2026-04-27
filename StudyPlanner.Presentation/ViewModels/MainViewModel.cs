using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using StudyPlanner.Application.Repositories;
using StudyPlanner.Core.Entities;
using System.Collections.ObjectModel;

namespace StudyPlanner.Presentation.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ISubjectRepository _subjectRepository;
        private readonly ILogger<MainViewModel> _logger;

        public MainViewModel(ISubjectRepository subjectRepository, ILogger<MainViewModel> logger)
        {
            _subjectRepository = subjectRepository;
            _logger = logger;
            LoadSubjectsCommand = new AsyncRelayCommand(LoadSubjectsAsync);
            AddSubjectCommand = new AsyncRelayCommand(AddSubjectAsync, CanAddSubject);

            // Load data on startup
            _ = LoadSubjectsCommand.ExecuteAsync(null);
        }

        [ObservableProperty] private ObservableCollection<Subject> _subjects = new();
        [ObservableProperty] private string _newSubjectName = string.Empty;
        [ObservableProperty] private int _newPriority = 1;

        public IAsyncRelayCommand LoadSubjectsCommand { get; }
        public IAsyncRelayCommand AddSubjectCommand { get; }

        private async Task LoadSubjectsAsync()
        {
            var list = await _subjectRepository.GetAllAsync();
            Subjects.Clear();
            foreach (var s in list) Subjects.Add(s);
        }

        private async Task AddSubjectAsync()
        {
#pragma warning disable CA1031 // Do not catch general exception types
            try
            {
                var subject = Subject.Create(NewSubjectName, NewPriority);
                await _subjectRepository.AddAsync(subject);
                NewSubjectName = string.Empty;
                NewPriority = 1;
                await LoadSubjectsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to add subject");
            }
#pragma warning restore CA1031 // Do not catch general exception types
        }

        private bool CanAddSubject() =>
            !string.IsNullOrWhiteSpace(NewSubjectName) && NewPriority > 0;

        partial void OnNewSubjectNameChanged(string value) => AddSubjectCommand.NotifyCanExecuteChanged();
        partial void OnNewPriorityChanged(int value) => AddSubjectCommand.NotifyCanExecuteChanged();
    }
}
