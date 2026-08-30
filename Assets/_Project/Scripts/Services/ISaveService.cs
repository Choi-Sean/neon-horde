namespace NeonHorde
{
    public interface ISaveService
    {
        MetaState Load();
        void Save(MetaState state);
        void Delete();
    }
}
