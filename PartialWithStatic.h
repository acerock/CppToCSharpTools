class PartialWithStatic
{
private:
    int m_instance;
    static const CString StaticArray[];
    
public:
    void HeaderMethod() { m_instance = 1; }
    void SourceMethod();
};