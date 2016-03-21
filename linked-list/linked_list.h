#pragma once
#include <stdint.h>
#include <memory>
//An unrolled linked list. It should have pretty darn good cache locality and have O(1) inserts and deletes.
//But, of course, it's a tradeoff. Inserting to the end is slower than a normal linked list because of the internal array shifts.
//However, it has none of the resizing overhead of a normal std::vector<> / List<T> and has much better cache locality than a normal
//linked list. 
template<class T> class linked_list
{
private:
	//The number of values to store in each node
	//(for cache locality)
	//MUST BE AN EVEN NUMBER (2,4,6,8...)
	static const int_fast32_t NODE_VALUE_COUNT = 64;
	static_assert(NODE_VALUE_COUNT % 2 == 0, "NODE_VALUE_COUNT must be a constant, even, number");
	//The number of nodes to keep in an internal buffer after adding and removing.
	//It should drastically reduce the number of malloc()'s in the list.
	static const int_fast32_t NODE_CACHE_SIZE = 16;
public:
	class node {
	public:
		node* next;
		node* prev;
		int_fast32_t count;
		T values[NODE_VALUE_COUNT];
		node() {

		}
		~node() {

		}
	};
private:
	node* node_cache[NODE_CACHE_SIZE];
	int_fast32_t node_cache_size = 0;

	//The number of elements currently stored in the list
	int_fast32_t _count = 0;
	node* first;
	node* last;

	node* get_unused_node() {
		if (node_cache_size == 0)
			return new node();

		//Avoid malloc-ing if possible
		node_cache_size--;
		auto val = node_cache[node_cache_size];
		//If you wanna be all PC about it:
		//node_cache[filled_node_cache_amount] = nullptr;
		return val;
	}

	void free_node(node* node) {
		if (node_cache_size == NODE_CACHE_SIZE - 1) { //cache full, just dealloc
			delete node; return;
		}
		node_cache[node_cache_size] = node;
		node->prev = nullptr;
		node->next = nullptr;
		node->count = 0;
		node_cache_size++;
	}

	//Combines a node with it's "next" node if possible. If the copy succeeded, the "next" node is removed from the list.
	bool combine_neighbor_blocks(node* lhs) {
		node* rhs = lhs->next;
		//There is no "next" node...
		if (rhs == nullptr) return false;
		//Check if the arrays are small enough
		if (lhs->count + rhs->count >= NODE_VALUE_COUNT)
			return false; //Arrays too large. Not copied.
						  //Copy over values
		memcpy((&lhs->values) + lhs->count, &rhs->values, rhs->count * sizeof(T));
		//Increment count
		lhs->count += rhs->count;
		//And patch out rhs from the list
		lhs->next = rhs->next;
		if (lhs->next != nullptr)
			lhs->next->prev = lhs;
		//And return rhs
		free_node(rhs);
		return true; //It was copied to lhs
	}

	//Traverses the node list to get the node containing the specified index, also returning the first "index" that is in the node
	//DOES NOT CHECK FOR ERRORS
	node* traverse_nodes(int index, int* return_for_node_lowest_addr) {
		//Now, it's a bit complicated, because individual nodes may not be full...
		//they may be half full or even empty. Not only that, but if index > count / 2, we need to traverse backward
		//But first, check if it's the first or last node
		if (index <= _count && index > _count - last->count) {
			*return_for_node_lowest_addr = _count - last->count;
			return last; //Optimize for last node first -- that way add to end is the fastest path
		}
		if (index >= 0 && index < first->count) {
			*return_for_node_lowest_addr = 0;
			return first;
		}


		if (index > _count / 2) {
			//Traverse last -> first
			node* n = last;
			//The index of the last element of the node
			int node_top_index = _count - 1;
			//Do a preliminary check to see if it's the last node in the set
			while (n->prev != nullptr) {
				//Check if the index is in the node
				if (index <= node_top_index && index > node_top_index - n->count) {
					*return_for_node_lowest_addr = node_top_index - n->count;
					return n;
				}
				node_top_index -= n->count;
				n = n->prev;
			}
		}
		else {
			//Traverse first -> last
			node* n = first;
			//The index of the last element of the node
			int node_bottom_index = 0;
			//Do a preliminary check to see if it's the last node in the set
			while (n->next != nullptr) {
				//Check if the index is in the node
				if (index >= node_bottom_index && index < node_bottom_index + n->count) {
					*return_for_node_lowest_addr = node_bottom_index;
					return n;
				}
				node_bottom_index += n->count;
				n = n->next;
			}
		}

		//Something is VERY VERY wrong. 
		throw "linked_list<T>->traverse_nodes() FATAL ERROR";
	}

	void split_node(node* block, int node_begin_ind) {

		//Create a new block and postfix it to current node
		auto nxt = block->next;
		auto new_node = get_unused_node();
		block->next = new_node;
		new_node->prev = block;
		new_node->next = nxt;
		if (nxt == nullptr) //This is the new end of the list
			last = new_node;
		//Copy half the contents of this block into the next
		block->count = NODE_VALUE_COUNT / 2;
		new_node->count = NODE_VALUE_COUNT / 2;
		memmove(&new_node->values, &block->values[NODE_VALUE_COUNT / 2], sizeof(T) * (NODE_VALUE_COUNT / 2));
	}
public:
	linked_list() {
		for (int i = 0; i < NODE_CACHE_SIZE; i++)
			node_cache[i] = nullptr;

		first = nullptr;
		last = nullptr;
	}
	~linked_list() {
		for (int i = 0; i < NODE_CACHE_SIZE; i++)
			delete node_cache[i];

		node* iter = first;
		while (iter != nullptr)
		{
			auto curr = iter;
			iter = iter->next;
			delete curr;
		}
	}

	int count() { return _count; }
	void clear() {
		node* n = first;
		while (n != nullptr) {
			auto tmp = n;
			n = n->next;
			free_node(tmp);
		}
		_count = 0;
	}

	//Puts the value in the list as fast as it can, regardless of position.
	//It could be item #3313 or item #10290...depends on which node is here
	void put_fast(T value) {
		//Fast case for empty
		if (first == nullptr) {
			first = get_unused_node();
			last = first;
			first->count = 1;
			_count = 1;
			first->values[0] = value;
			return;
		}

		if (first->count == NODE_VALUE_COUNT) {
			//Split in two
			split_node(first, 0);
		}

		first->count++;
		first->values[first->count] = value;
		_count++;
	}

	void put_first(T value) { put_at(0, value); }

	void put_last(T value) { put_at(_count, value); }

	void put_at(int index, T value) {
		//Special case for put_first -- on an empty list, 
		//it will fail the next check but still should be valid
		if (index == 0 && first == nullptr) {
			//It's a new list, we're just starting
			first = get_unused_node();
			first->count = 1;
			_count++;
			first->values[0] = value;
			last = first;
			return;
		}
		if (index < 0 || index > _count)
			throw "linked_list<T>->put_at(int, T): index out of range.";

		//Unfortunately, we have to traverse the node tree
		//Find what block to store it in
		int node_begin_ind = 0;
		node* block = traverse_nodes(index, &node_begin_ind);

		//Again, slow case: check if the node is full
		if (block->count == NODE_VALUE_COUNT) {
			split_node(block, node_begin_ind);
			//And, if we're now supposed to point at the next node:
			if (index >= node_begin_ind + block->count) {
				node_begin_ind = node_begin_ind + block->count;
				block = block->next;
			}
		}
		//Slow case: make room before putting the item
		if (block->count != index - node_begin_ind) {
			int offset_in_node = index - node_begin_ind;
			int number_of_items_to_copy = block->count - offset_in_node;
			//Shift the contents of the node one item right to make room
			memmove(&block->values[offset_in_node + 1],
				&block->values[offset_in_node], number_of_items_to_copy * sizeof(T));
		}
		//Actually insert -- this is pretty fast
		block->values[index - node_begin_ind] = value;
		block->count += 1;
		_count++;

		//And if the block is not full, try to combine with next block for cache locality and lower memory usage
		combine_neighbor_blocks(block);
	}

	void remove_first() {
		remove_at(0);
	}

	void remove_last() {
		remove_at(_count - 1);
	}

	void remove_at(int index) {
		if (index >= _count || index < 0) {
			throw "linked_list<T>->remove_at(int): index out of range";
		}

		int node_offset = 0;
		node* n = traverse_nodes(index, &node_offset);
		//Fast case: just remove the node
		if (n->count == 1) {
			if (n->next != nullptr)
				n->next->prev = n->prev;
			if (n->prev != nullptr)
				n->prev->next = n->next;
			//If it's the first or last node
			if (n == first)
				first = n->next;
			if (n == last)
				last = n->prev;

			free_node(n);
			_count--;
			return;
		}

		//See if it's stored in the middle of the node
		if (index != n->count - 1) {
			int count_to_copy = n->count - index;
			//Shift the backing array 1 item left.
			memmove((&n->values) + index, (&n->values) + index + 1, count_to_copy * sizeof(T));
		}

		//No need to copy over the last element. We just decrement the count
		n->count--;
		_count--;

		//And if possible, optimize memory usage
		combine_neighbor_blocks(n);
	}

	inline T operator [](int index) {
		if (index >= _count || index < 0) {
			throw "linked_list<T>->operator[int]: index out of range";
		}
		int node_begin = 0;
		node* n = traverse_nodes(index, &node_begin);
		return n->values[index - node_begin];
	}

	//Iterator for the linked list
	class iterator : std::iterator<std::bidirectional_iterator_tag, T> {
	private:
		linked_list* list;
		node* curr_node;
		int node_offset;
	public:
		iterator(linked_list& list) {
			this->list = list;
			curr_node = list->first;
			node_offset = 0;
		}

		iterator(linked_list* list) {
			this->list = list;
			curr_node = list->first;
			node_offset = 0;
		}

		iterator(const iterator& iter) {
			list = iter->list;
			curr_node = iter->curr_node;
			node_offset = iter->node_offset;
		}
		~iterator() {}

		inline iterator& operator++() {
			node_offset++;
			if (node_offset >= curr_node->count) {
				if (curr_node->next == nullptr)
				{
					node_offset = curr_node->count - 1;
					return *this;
				}
				curr_node = curr_node->next;
				node_offset = 0;
			}
			return *this;
		}

		inline iterator operator++(int) { iterator tmp(*this); operator++(); return tmp; }

		inline iterator& operator--() {
			node_offset--;
			if (node_offset < 0) {
				if (curr_node->prev == nullptr)
				{
					node_offset = 0;
					return *this;
				}
				curr_node = curr_node->prev;
				node_offset = curr_node->count - 1;
			}
			return *this;
		}

		inline iterator operator--(int) { iterator tmp(*this); operator++(); return tmp; }

		inline T next() {
			operator++();
			return curr_node->values[node_offset];
		}
		inline T prev() {
			operator--();
			return curr_node->values[node_offset];
		}

		inline void skip(int count) {
			for (int i = 0; i < countl i++)
				next();
		}

		//Returns whether the iterator is at the end
		inline bool end() {
			return curr_node->next == nullptr && curr_node->count - 1 == node_offset;
		}
		//Returns whether the interator is at the beginning
		inline bool begin() {
			return curr_node->prev == nullptr && node_offset == 0;
		}

		inline bool has_next() { return !end(); }

		inline bool operator ==(const iterator& rhs) { return list == rhs->list && curr_node == rhs->curr_node && node_offset == rhs->node_offset; }
		inline bool operator !=(const iterator& rhs) { return !(operator==(rhs)); }

		inline T& operator*() { return curr_node->values[node_offset]; }
	};

	//Method to get the iterator
	inline iterator iter() { return iterator(this); }
};

