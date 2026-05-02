import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { getProducts } from '../api/catalogApi';
import { useCartStore } from '../store/cartStore';
import { Product } from '../types';

export default function CatalogPage() {
  const { data, isLoading, isError } = useQuery({
    queryKey: ['products'],
    queryFn: () => getProducts().then(r => r.data),
  });

  const addItem = useCartStore(s => s.addItem);
  const [added, setAdded] = useState<string | null>(null);

  const handleAdd = (product: Product) => {
    addItem(product);
    setAdded(product.id);
    setTimeout(() => setAdded(null), 1200);
  };

  if (isLoading) {
    return <div className="text-center py-16 text-gray-500">Loading products...</div>;
  }

  if (isError) {
    return <div className="text-center py-16 text-red-500">Failed to load products.</div>;
  }

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Products</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
        {data?.map((product: Product) => (
          <div key={product.id} className="bg-white rounded-xl shadow p-5 flex flex-col">
            <span className="text-xs font-medium text-indigo-500 uppercase tracking-wide mb-1">
              {product.category}
            </span>
            <h2 className="font-semibold text-gray-800 text-base flex-1">{product.name}</h2>
            <div className="flex items-center justify-between mt-4">
              <span className="text-xl font-bold text-gray-900">
                €{product.price.toFixed(2)}
              </span>
              <button
                onClick={() => handleAdd(product)}
                className={`px-4 py-1.5 rounded-lg text-sm transition-colors text-white ${
                  added === product.id
                    ? 'bg-green-500'
                    : 'bg-indigo-600 hover:bg-indigo-700'
                }`}
              >
                {added === product.id ? '✓ Added' : 'Add to Cart'}
              </button>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
