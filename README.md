# Introduction 
Example of a reusable library (as a nuget package). Takes an available 3rs party library and wraps it behind an interface.
This provides a number of advantages, including
1. Wiring into the logging mechanism used
2. Developers do not need to learn the specifics of how to implement the 3rd party library (but at the same time provides examples on how to use it, if needed to extend)
3. Provides a seemless means of upgrading the library if the underlaying 3rd party library is updated. (so rollout of the library can be done in a versioned approach)
4. If the 3rd party library becomes deprecated in some way, you can seemlessly replace with with a different implementation of the functionality it provides with the only requirement of solutions that rely on it being that the version is updated.
