class MultiFileClass
{
private:
    int m_value1;
    int m_value2;
    
public:
    MultiFileClass();
    
    // Inline method
    int GetSum() { return m_value1 + m_value2; }
    
    // Methods to be implemented in source files
    void MethodFromFile1();
    void MethodFromFile2();
};