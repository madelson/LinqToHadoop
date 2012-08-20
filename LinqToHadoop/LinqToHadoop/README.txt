Structure:

IEncoder:
has write methods for primitive types. Supports text, rawbytes, and typedbytes (different IEncoders)
IDecoder:
opposite of IEncoder
these take a config as input!
TODO diff types for text key/value encoder or is that an input (e. g. key, value, or raw?)

Records are broken into keys and values
Keys and values are broken into fields. The encoders have Read/Write methods for each field type (basically primitives + IEnumerables and maps)
* BeginWritingKey
* Write...
* BeginValue
* Write...
...
* Flush/Close (writes out the final newline)

* BeginReadingKey
* Read...
* BeginReadingValue
* Read...
...

* To handle maps/lists, BeginCollection(type, size)

Serializer library generates a (de)serializer for type KeyValuePair<TKey, TValue>

Text encoding:
keys:
the separator is replaced with itself -1 (can't be zero :)) + the last char, and the last char is replaced with itself twice
values:
the separator is replaced with a subsititute, which itself is replaced with a substitute
(e. g. # => #R, sep => #S)

MapReduceReader/Writer
can read/write keyValuePairs given and I[De/En]coder & a config

Serialization:
- create serializers/deserializers for complex types, including anonymous types (uses the map-reduce reader/writer under the hood)