# HashTableRx
A Reactive Hash Table, used to store and observe values in a hash table reflecting the structure of an object.

## Usage

```csharp
// Replace YOUR_OBJECT with the object you want to store in the hash table
var obj = new YOUR_OBJECT();

// Create a new Hash Table - the bool is used to determine if the hash table should use Upper Case or Case Sensitive keys
var hashTable = new HashTableRx(false);

// Set / update the structure of the Hash Table
hashTable.SetStructure(obj);

hashTable.Observe<bool>("YourProperty").Subscribe(value =>
{
    // Do something with the value
});

// Set the value in the hash table
hashTable.Value<float>("YourProperty", 10.5f);

// Get the value from the hash table
var value = hashTable.Value<float>("YourProperty");

// Get the current structure of the hash table
var updatedObj = hashTable.GetStructure();
```

## About

The HashTableRx can be used to store and observe values in a hash table reflecting the structure of an object. The HashTableRx is a reactive hash table, which means that you can observe the values in the hash table and react to changes in the values. The HashTableRx is a generic class, which means that you can store any type of object in the hash table. The HashTableRx is a lightweight and easy-to-use library that can be used in any C# project.

Variables in the HashTableRx are seperated by a dot, for example: "YourProperty.SubProperty".
This is used to reflect the layers of a structured object in the hash table.

Currently, the HashTableRx supports the following types:
Structures made of Public Fields both Primative and further layers of classes, itteration will be done to find the Primative values in a Structure.

ToDo: Add support for Properties.
