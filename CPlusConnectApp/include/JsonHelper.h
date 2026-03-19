#pragma once
#include <string>      // For std::string
#include <vector>      // For std::vector
#include <map>         // For std::map
#include <memory>      // For std::unique_ptr
#include <typeinfo>    // For typeid
#include <iostream>

// Base class for all properties (non-templated)
class JsonPropertyBase {
public:
    std::string Name;
    virtual ~JsonPropertyBase() = default;
    virtual const std::type_info& GetTypeInfo() const = 0;
};

// Templated property inherits from base
template <typename MemberType>
class JsonProperty : public JsonPropertyBase {
public:
    MemberType ValueType;

    JsonProperty(const std::string& name) {
        this->Name = name;
    }

    const std::type_info& GetTypeInfo() const override {
        return typeid(MemberType);
    }
};



template <typename ClassType>
class JsonRegister {
public:
    // Register takes any property that derives from JsonPropertyBase
    static void Register(std::vector<JsonPropertyBase*> Properties) {
        // Get or create the register for this type
        JsonRegister* reg = getOrCreateRegister();

        // Store all properties
        for (auto* prop : Properties) {
            reg->properties.push_back(std::unique_ptr<JsonPropertyBase>(prop));
        }
    }

    const std::vector<std::unique_ptr<JsonPropertyBase>>& GetProperties() const {
        return properties;
    }

    // Static registry of all registers - this needs moved outside of this class later so I dont need to call a random type just to access
    static std::map<std::string, JsonRegister*>& getRegistry() {
        static std::map<std::string, JsonRegister*> registry;
        return registry;
    }
private:
    static JsonRegister* getOrCreateRegister() {
        std::string typeName = typeid(ClassType).name();
        auto& registry = getRegistry();

        if (registry.find(typeName) == registry.end()) {
            registry[typeName] = new JsonRegister();
            registry[typeName]->typeName = typeName;
        }
        return registry[typeName];
    }

    ClassType Type;  // Prototype instance
    std::string typeName;
    std::vector<std::unique_ptr<JsonPropertyBase>> properties;  // Store any property type
};

class JsonHelper
{

	// stores json schematics, and checks the list and then returns what type to deserilize into 
public:
    //static void Visualize() {


    //}
};

