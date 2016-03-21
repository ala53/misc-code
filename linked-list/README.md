# C++ Unrolled Linked List

A single header file implementation of a doubly linked list. It is an unrolled list, meaning that each `node` contains a small backing array (defaults to 64 items per node).
Also, it keeps a small node cache (~16 items) so that the overhead of `malloc()`-ing new nodes can be avoided during mixed insert/remove scenarios (such as queues or stacks).

When calling `put`, what happens is:
 - If the list is empty, take a fast path
 - Otherwise, get the node which contains the index we want to place the value in
 - If all the slots in that node are full, get a node from the unused node cache (or create if the cache is empty). Then, move half the contents to the new node.
 - If the index requested *is not* the last index in the node, copy all of the items one right to make room (basically, a normal list shift)
 - And insert the value into the list.

### Benefits
 * Inserts/removes to/from the head or tail are extremely fast (only a couple of times slower than `array[item] = value`)
 * Lower overhead than a normal linked list (2 extra pointers per 64 values rather than 2 pointers per value).
 * Most inserts and deletes can be done without memory allocation (just like a normal list) 

### Tradeoffs
Unfortunately, the above design comes with a few tradeoffs:
 * Insertions to the center are quite slow (have to traverse over half the nodes)