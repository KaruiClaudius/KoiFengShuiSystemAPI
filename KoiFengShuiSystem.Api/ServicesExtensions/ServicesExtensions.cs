using KoiFengShuiSystem.DataAccess.Base;

namespace KoiFengShuiSystem.Api.ServicesExtensions
{
    public static class ServicesExtensions
    {
        public static IServiceCollection AddRepositories(this IServiceCollection services)
        {
            services.AddScoped(typeof(GenericRepository<>));
            //services.AddScoped<IUnitOfWorkRepository, UnitOfWorkRepository>();
            //services.AddScoped<IUserRepository, UserRepository>();
            //services.AddScoped<ISubjectRepository, SubjectRepository>();
            //services.AddScoped<ISlotRepository, SlotRepository>();
            //services.AddScoped<IBlogRepository, BlogRepository>();
            //services.AddScoped<IClassRepository, ClassRepository>();
            //services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
            //services.AddScoped<IConsultationRequestRepository, ConsultationRequestRepository>();
            //services.AddScoped<ITransactionRepository, TransactionRepository>();
            //services.AddScoped<ITutorDegreeRepository, TutorDegreeRepository>();
            //services.AddScoped<ISlotStudentRepository, SlotStudentRepository>();
            //services.AddScoped<IRequestWithDrawRepository, RequestWithDrawRepository>();
            //services.AddScoped<IFAQRepository, FAQRepository>();
            //services.AddScoped<INotificationRepository, NotificationRepository>();
            //services.AddScoped<IStudentClassRepository, StudentClassRepository>();
            //services.AddScoped<ITutorSubjectRepository, TutorSubjectRepository>();
            //services.AddScoped<ITutorVideoRepository, TutorVideoRepository>();
            services.AddProblemDetails();
            services.AddLogging();

            return services;
        }

        public static IServiceCollection AddGeneralServices(this IServiceCollection services)
        {
            //services.AddScoped<IUserServices, UserServices>();
            //services.AddScoped<ISubjectService, SubjectService>();
            //services.AddScoped<IBlogService, BlogService>();
            //services.AddScoped<IClassServices, ClassServices>();
            //services.AddScoped<IConsultationRequestService, ConsultationRequestService>();
            //services.AddScoped<IAuthServices, AuthServices>();
            //services.AddScoped<IFirebaseUploadServices, FirebaseUploadServices>();
            //services.AddScoped<IFireBaseAuthServices, FirebaseAuthServices>();
            //services.AddScoped<IVnPayServices, VnPayServices>();
            //services.AddScoped<ISlotServices, SlotService>();
            //services.AddScoped<ISlotStudentServices, SlotStudentService>();
            //services.AddScoped<ITransactionServices, TransactionServices>();
            //services.AddScoped<INotificationService, NotificationService>();
            //services.AddScoped<ITutorDegreeService, TutorDegreeService>();
            //services.AddScoped<IPaymentProcessor, VnPayProcessor>();
            //services.AddScoped<IRequestWithDrawServices, RequestWithDrawServices>();
            //services.AddScoped<IFAQService, FAQService>();
            //services.AddScoped<IStudentClassService, StudentClassService>();
            //services.AddScoped<ITutorSubjectService, TutorSubjectService>();
            //services.AddScoped<ITutorVideoService, TutorVideoService>();

            //services.AddTransient<IEmailServices, EmailServices>();
            //services.AddTransient<IJwtProviderServices, JwtProviderServices>();
            //services.AddProblemDetails();
            //services.AddLogging();
            return services;
        }
    }
}
